using System.Net;
using System.Text.Json;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.ExternalServices;
using PaymentGateway.Infrastructure.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using RichardSzalay.MockHttp;

namespace PaymentGateway.Infrastructure.UnitTests.ExternalServices
{
    public class SimulatedAcquiringBankTests
    {
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<ILogger<SimulatedAcquiringBank>> _mockLogger = new();
        private readonly MockHttpMessageHandler _mockHttpHandler = new();

        private SimulatedAcquiringBank _sut;

        private readonly IOptions<RetryPolicyOptions> _retryOptions = Microsoft.Extensions.Options.Options.Create(new RetryPolicyOptions
        {
            RetryCount = 0,
            CircuitBreaker = new CircuitBreakerOptions
            {
                BreakDurationSeconds = 10,
                FailureThreshold = 6
            }
        });

        private readonly IOptions<AcquiringBankOptions> _bankOptions = Microsoft.Extensions.Options.Options.Create(new AcquiringBankOptions
        {
            BaseUrl = "http://localhost:8080"
        });

        public SimulatedAcquiringBankTests()
        {
            HttpClient httpClient = new(_mockHttpHandler)
            {
                BaseAddress = new Uri(_bankOptions.Value.BaseUrl)
            };

            _sut = new SimulatedAcquiringBank(httpClient, _mockMapper.Object, _mockLogger.Object, _retryOptions, _bankOptions);
        }

        #region Successful Execution

        [Fact(DisplayName = "AuthorizePaymentAsync returns mapped BankResponse when successful")]
        public async Task AuthorizePaymentAsync_ShouldReturnMappedResponse_WhenSuccess()
        {
            // Arrange
            var bankRequest = new BankRequest { Amount = 1000, Currency = nameof(Currency.GBP) };
            var acquiringRequest = new AcquiringBankRequest();
            var acquiringResponse = CreateValidBankResponse();
            var json = JsonSerializer.Serialize(acquiringResponse);

            _mockMapper.Setup(m => m.Map<AcquiringBankRequest>(bankRequest)).Returns(acquiringRequest);

            _mockHttpHandler
                .When(HttpMethod.Post, "http://localhost:8080/payments")
                .Respond("application/json", json);

            _mockMapper.Setup(m => m.Map<BankResponse>(It.IsAny<AcquiringBankResponse>()))
                .Returns((AcquiringBankResponse src) => new BankResponse
                {
                    Authorized = src.Authorized, 
                    AuthorizationCode = src.AuthorizationCode
                });

            // Act
            var result = await _sut.AuthorizePaymentAsync(bankRequest);

            // Assert
            result.Should().NotBeNull();
            result.Authorized.Should().BeTrue();
            result.AuthorizationCode.Should().Be("e10c6c81-330e-44f8-b5f5-9e74053702f8");
        }

        #endregion

        #region Failure Scenarios

        [Fact(DisplayName = "AuthorizePaymentAsync throws HttpRequestException on BadRequest response")]
        public async Task AuthorizePaymentAsync_ShouldThrowHttpRequestException_OnBadRequestResponse()
        {
            // Arrange
            var bankRequest = new BankRequest();

            _mockMapper.Setup(m => m.Map<AcquiringBankRequest>(It.IsAny<BankRequest>()))
                .Returns(new AcquiringBankRequest());

            _mockHttpHandler
                .When(HttpMethod.Post, "http://localhost:8080/payments")
                .Respond(HttpStatusCode.BadRequest);

            // Act
            Func<Task> act = () => _sut.AuthorizePaymentAsync(bankRequest);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Theory(DisplayName = "AuthorizePaymentAsync throws InvalidOperationException on malformed JSON response")]
        [InlineData("{ invalid json }")]
        [InlineData("123")]
        [InlineData("null")]
        [InlineData("{\"authorized\": true")]
        [InlineData("[{ \"unexpected\": \"structure\" }]")]
        public async Task AuthorizePaymentAsync_ShouldThrow_OnDeserializationFailure(string invalidJson)
        {
            // Arrange
            var bankRequest = new BankRequest();
            var acquiringRequest = new AcquiringBankRequest();

            _mockMapper.Setup(m => m.Map<AcquiringBankRequest>(It.IsAny<BankRequest>()))
                .Returns(acquiringRequest);

            _mockHttpHandler
                .When(HttpMethod.Post, "http://localhost:8080/payments")
                .Respond("application/json", invalidJson);

            // Act
            Func<Task> act = () => _sut.AuthorizePaymentAsync(bankRequest);

            // Assert
            if (invalidJson == "null")
            {
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Invalid response from acquiring bank. The deserialized response is null.");
            }
            else
            {
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Invalid response from acquiring bank. The response could not be deserialized.");
            }
        }

        #endregion

        #region Timeout Handling

        [Fact(DisplayName = "AuthorizePaymentAsync throws TimeoutRejectedException when HTTP request exceeds timeout")]
        public async Task AuthorizePaymentAsync_ShouldThrowTimeout_WhenHttpCallTakesTooLong()
        {
            // Arrange
            var retryOptions = CreateRetryOptions(0, 5, 10);
            retryOptions.Value.TimeoutSeconds = 1;

            _sut = CreateSutWithRetryOptions(retryOptions);

            var bankRequest = new BankRequest();
            var acquiringRequest = new AcquiringBankRequest();

            _mockMapper.Setup(m => m.Map<AcquiringBankRequest>(It.IsAny<BankRequest>()))
                .Returns(acquiringRequest);

            _mockHttpHandler
                .When(HttpMethod.Post, "http://localhost:8080/payments")
                .Respond(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Deliberately exceed timeout
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });

            // Act
            Func<Task> act = () => _sut.AuthorizePaymentAsync(bankRequest);

            // Assert
            await act.Should().ThrowAsync<TimeoutRejectedException>();
        }

        #endregion

        #region Retry and Circuit Breaker Policy

        [Fact(DisplayName = "AuthorizePaymentAsync retries on transient errors and eventually succeeds")]
        public async Task AuthorizePaymentAsync_ShouldRetry_OnTransientError_ThenSucceed()
        {
            // Arrange
            var bankRequest = new BankRequest();
            var acquiringRequest = new AcquiringBankRequest();
            var acquiringResponse = CreateValidBankResponse();
            var json = JsonSerializer.Serialize(acquiringResponse);
            var retryOptions = CreateRetryOptions(2, 60, 5);

            _sut = CreateSutWithRetryOptions(retryOptions);

            _mockMapper.Setup(m => m.Map<AcquiringBankRequest>(bankRequest)).Returns(acquiringRequest);
            _mockMapper.Setup(m => m.Map<BankResponse>(It.IsAny<AcquiringBankResponse>()))
                .Returns(new BankResponse
                {
                    Authorized = true,
                    AuthorizationCode = acquiringResponse.AuthorizationCode
                });

            int callCount = 0;

            _mockHttpHandler
                .When(HttpMethod.Post, "http://localhost:8080/payments")
                .Respond(_ =>
                {
                    callCount++;
                    return callCount <= 2
                        ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                        : new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                        };
                });

            // Act
            var result = await _sut.AuthorizePaymentAsync(bankRequest);

            // Assert
            result.Should().NotBeNull();
            result.Authorized.Should().BeTrue();
            callCount.Should().Be(3);
        }

        [Fact(DisplayName = "CircuitBreaker opens after consecutive transient failures")]
        public async Task CircuitBreaker_ShouldOpen_AfterConsecutiveFailures()
        {
            // Arrange
            var bankRequest = new BankRequest();
            var acquiringRequest = new AcquiringBankRequest();

            _mockMapper.Setup(m => m.Map<AcquiringBankRequest>(It.IsAny<BankRequest>()))
                .Returns(acquiringRequest);

            int callCount = 0;

            _mockHttpHandler
                .When(HttpMethod.Post, "http://localhost:8080/payments")
                .Respond(_ =>
                {
                    callCount++;
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                });

            var failureThreshold = _retryOptions.Value.CircuitBreaker.FailureThreshold;

            // Act: trigger failure threshold
            for (int i = 0; i < failureThreshold; i++)
            {
                await Assert.ThrowsAsync<HttpRequestException>(FailingCall);
            }

            var exception = await Record.ExceptionAsync(FailingCall);

            // Assert
            exception.Should().BeOfType<BrokenCircuitException>()
                .Which.Message.Should().Contain("The circuit is now open");

            callCount.Should().Be(failureThreshold);
            return;

            async Task FailingCall() => await _sut.AuthorizePaymentAsync(bankRequest);
        }

        [Fact(DisplayName = "CircuitBreaker resets after break duration and allows successful retry")]
        [Trait("Category", "Slow")] // Because of Task.Delay
        public async Task CircuitBreaker_ShouldReset_AfterBreakDuration()
        {
            // Arrange
            var bankRequest = new BankRequest();
            var acquiringRequest = new AcquiringBankRequest();
            var acquiringResponse = CreateValidBankResponse();

            _mockMapper.Setup(m => m.Map<AcquiringBankRequest>(It.IsAny<BankRequest>()))
                .Returns(acquiringRequest);

            _mockMapper.Setup(m => m.Map<BankResponse>(It.IsAny<AcquiringBankResponse>()))
                .Returns(new BankResponse
                {
                    Authorized = true,
                    AuthorizationCode = acquiringResponse.AuthorizationCode
                });

            int callCount = 0;

            _mockHttpHandler
                .When(HttpMethod.Post, "http://localhost:8080/payments")
                .Respond(_ =>
                {
                    callCount++;
                    if (callCount <= _retryOptions.Value.CircuitBreaker.FailureThreshold)
                        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

                    var json = JsonSerializer.Serialize(acquiringResponse);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                });

            // Act: trip the circuit
            for (int i = 0; i < _retryOptions.Value.CircuitBreaker.FailureThreshold; i++)
            {
                await Assert.ThrowsAsync<HttpRequestException>(FailingCall);
            }

            var circuitOpenException = await Record.ExceptionAsync(FailingCall);
            circuitOpenException.Should().BeOfType<BrokenCircuitException>();

            // Wait for reset period
            await Task.Delay(TimeSpan.FromSeconds(_retryOptions.Value.CircuitBreaker.BreakDurationSeconds + 1));

            // Retry should succeed
            var result = await _sut.AuthorizePaymentAsync(bankRequest);

            // Assert
            result.Should().NotBeNull();
            result.Authorized.Should().BeTrue();
            return;

            async Task FailingCall() => await _sut.AuthorizePaymentAsync(bankRequest);
        }

        #endregion

        #region Failure Scenarios

        [Fact(DisplayName = "AuthorizePaymentAsync throws InvalidOperationException when mapper returns null")]
        public async Task AuthorizePaymentAsync_ShouldThrowInvalidOperationException_WhenMapperReturnsNull()
        {
            // Arrange
            var bankRequest = new BankRequest { Amount = 1000, Currency = nameof(Currency.GBP) };
            var acquiringRequest = new AcquiringBankRequest();
            var acquiringResponse = CreateValidBankResponse();
            var json = JsonSerializer.Serialize(acquiringResponse);
            
            _mockMapper.Setup(m => m.Map<AcquiringBankRequest>(It.IsAny<BankRequest>())).Returns(acquiringRequest);
            _mockMapper.Setup(m => m.Map<BankResponse>(It.IsAny<AcquiringBankResponse>())).Returns((BankResponse)null!);

            _mockHttpHandler
                .When(HttpMethod.Post, "http://localhost:8080/payments")
                .Respond("application/json", json);

            // Act
            Task Act() => _sut.AuthorizePaymentAsync(bankRequest);

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(Act);
            exception.Message.Should().Be("Could not map acquiring bank response to BankResponse. The mapping returned null.");
        }

        #endregion
        
        #region Test Helpers

        private static AcquiringBankResponse CreateValidBankResponse() => new()
        {
            Authorized = true,
            AuthorizationCode = "e10c6c81-330e-44f8-b5f5-9e74053702f8"
        };

        private SimulatedAcquiringBank CreateSutWithRetryOptions(IOptions<RetryPolicyOptions> retryPolicyOptions)
        {
            var httpClient = new HttpClient(_mockHttpHandler)
            {
                BaseAddress = new Uri(_bankOptions.Value.BaseUrl)
            };

            return new SimulatedAcquiringBank(httpClient, _mockMapper.Object, _mockLogger.Object, retryPolicyOptions, _bankOptions);
        }

        private static IOptions<RetryPolicyOptions> CreateRetryOptions(int retryCount, int failureThreshold, int breakDurationSeconds)
        {
            return Microsoft.Extensions.Options.Options.Create(new RetryPolicyOptions
            {
                RetryCount = retryCount,
                CircuitBreaker = new CircuitBreakerOptions
                {
                    FailureThreshold = failureThreshold,
                    BreakDurationSeconds = breakDurationSeconds
                }
            });
        }

        #endregion
    }
}
