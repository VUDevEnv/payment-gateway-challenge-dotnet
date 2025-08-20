using FluentValidation;
using FluentValidation.Results;
using PaymentGateway.Application.Commands;
using PaymentGateway.Application.Constants;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Interfaces.AcquiringBank;
using PaymentGateway.Domain.Models;

namespace PaymentGateway.Application.UnitTests.Commands
{
    public class ProcessPaymentCommandHandlerTests
    {
        private readonly Mock<IValidator<PostPaymentRequest>> _mockValidator = new();
        private readonly Mock<IAcquiringBank> _mockAcquiringBank = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<IPaymentRepository> _mockPaymentRepository = new();
        private readonly Mock<IIdempotencyService> _mockIdempotencyService = new();
        private readonly Mock<ILogger<ProcessPaymentCommandHandler>> _mockLogger = new();

        private readonly ProcessPaymentCommandHandler _handler;

        public ProcessPaymentCommandHandlerTests()
        {
            _handler = new ProcessPaymentCommandHandler(
                _mockValidator.Object,
                _mockIdempotencyService.Object,
                _mockAcquiringBank.Object,
                _mockMapper.Object,
                _mockPaymentRepository.Object,
                _mockLogger.Object);
        }

        #region Validation Tests

        [Fact(DisplayName = "HandleAsync throws ArgumentNullException when request is null")]
        public async Task HandleAsync_ThrowsArgumentNullException_WhenRequestIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleAsync(null!, null!));
        }

        [Fact(DisplayName = "HandleAsync should throw ValidationException when request is invalid")]
        public async Task HandleAsync_ShouldThrowValidationException_WhenRequestIsInvalid()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();

            var failures = new List<ValidationFailure>
            {
                new(nameof(PostPaymentRequest.CardNumber), ValidationMessages.CardNumberRequired),
                new(nameof(PostPaymentRequest.ExpiryMonth), ValidationMessages.ExpiryMonthRequired),
                new(nameof(PostPaymentRequest.ExpiryYear), ValidationMessages.ExpiryYearRequired),
                new(nameof(PostPaymentRequest.Currency), ValidationMessages.CvvRequired),
                new(nameof(PostPaymentRequest.Amount), ValidationMessages.AmountRequired),
                new(nameof(PostPaymentRequest.Cvv), ValidationMessages.CvvRequired)
            };

            _mockValidator.Setup(v => v.ValidateAsync(request, CancellationToken.None))
                .ReturnsAsync(new ValidationResult(failures));

            // Act
            Func<Task> act = async () => await _handler.HandleAsync(request, null!);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        #endregion

        #region Bank Response Scenarios

        [Fact(DisplayName = "HandleAsync should decline and save payment when bank declines it")]
        public async Task HandleAsync_ShouldDeclineAndSavePayment_WhenBankDeclines()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            var bankRequest = new BankRequest();
            var bankResponse = new BankResponse { Authorized = false, AuthorizationCode = string.Empty };
            var payment = CreateTestPayment(request, bankResponse);

            _mockValidator.Setup(v => v.ValidateAsync(request, CancellationToken.None))
                .ReturnsAsync(new ValidationResult());

            _mockMapper.Setup(m => m.Map<BankRequest>(request))
                .Returns(bankRequest);

            _mockAcquiringBank.Setup(b => b.AuthorizePaymentAsync(bankRequest, CancellationToken.None))
                .ReturnsAsync(bankResponse);

            _mockMapper.Setup(m => m.Map<PostPaymentResponse>(It.IsAny<Payment>()))
                .Returns(new PostPaymentResponse { Id = payment.Id, Status = PaymentStatusDto.Declined });

            _mockPaymentRepository.Setup(p => p.SavePaymentAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request, null!);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PaymentStatusDto.Declined);
            _mockPaymentRepository.Verify(p => p.SavePaymentAsync(It.Is<Payment>(x => x.Status == PaymentStatus.Declined)), Times.Once);
        }

        [Fact(DisplayName = "HandleAsync should authorize and save payment when bank authorizes it")]
        public async Task HandleAsync_ShouldAuthorizeAndSavePayment_WhenBankAuthorizes()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            var bankRequest = new BankRequest();
            var bankResponse = new BankResponse { Authorized = true, AuthorizationCode = "e10c6c81-330e-44f8-b5f5-9e74053702f8" };
            var payment = CreateTestPayment(request, bankResponse);

            _mockValidator.Setup(v => v.ValidateAsync(request, CancellationToken.None))
                .ReturnsAsync(new ValidationResult());

            _mockMapper.Setup(m => m.Map<BankRequest>(request))
                .Returns(bankRequest);

            _mockAcquiringBank.Setup(b => b.AuthorizePaymentAsync(bankRequest, CancellationToken.None))
                .ReturnsAsync(bankResponse);

            _mockMapper.Setup(m => m.Map<PostPaymentResponse>(It.IsAny<Payment>()))
                .Returns(new PostPaymentResponse { Id = payment.Id, Status = PaymentStatusDto.Authorized });

            _mockPaymentRepository.Setup(p => p.SavePaymentAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request, null!);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PaymentStatusDto.Authorized);
            _mockPaymentRepository.Verify(p => p.SavePaymentAsync(It.Is<Payment>(x => x.Status == PaymentStatus.Authorized)), Times.Once);
        }

        #endregion

        #region Idempotency Behavior Tests

        [Fact(DisplayName = "HandleAsync returns cached response when idempotency record exists with same hash")]
        public async Task HandleAsync_ReturnsCachedResponse_WhenIdempotencyRecordExistsWithSameHash()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            const string idempotencyKey = "b35f2757-b036-4ef1-888d-e2624e8fc914";
            var requestHash = ComputeHash(request);
            var cachedResponse = new PostPaymentResponse { Id = Guid.NewGuid(), Status = PaymentStatusDto.Authorized };

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<PostPaymentRequest>(), CancellationToken.None))
                .ReturnsAsync(new ValidationResult());

            _mockIdempotencyService
                .Setup(x => x.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, idempotencyKey))
                .ReturnsAsync((cachedResponse, requestHash));

            // Act
            var result = await _handler.HandleAsync(request, idempotencyKey);

            // Assert
            result.Should().BeEquivalentTo(cachedResponse);
            _mockLogger.Verify(logger =>
                    logger.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Returning cached response")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
            _mockPaymentRepository.Verify(p => p.SavePaymentAsync(It.IsAny<Payment>()), Times.Never);
        }

        [Fact(DisplayName = "HandleAsync throws when idempotency key reused with different request hash")]
        public async Task HandleAsync_Throws_WhenIdempotencyKeyUsedWithDifferentRequestPayload()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            const string idempotencyKey = "e5afe74b-3b4f-4d8f-a87a-579bcc1c7e17";

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<PostPaymentRequest>(), CancellationToken.None))
                .ReturnsAsync(new ValidationResult());

            _mockIdempotencyService
                .Setup(x => x.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, idempotencyKey))
                .ThrowsAsync(new InvalidOperationException("Idempotency-Key has already been used with a different request payload."));

            // Act
            Func<Task> act = async () => await _handler.HandleAsync(request, idempotencyKey);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Idempotency-Key has already been used with a different request payload.");
            _mockPaymentRepository.Verify(p => p.SavePaymentAsync(It.IsAny<Payment>()), Times.Never);
        }

        [Fact(DisplayName = "HandleAsync saves idempotency record after processing new payment")]
        public async Task HandleAsync_SavesIdempotencyRecord_AfterNewPaymentProcessed()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            const string idempotencyKey = "204ed32a-ddbd-4058-be1d-aa2bd66f5370";
            var bankRequest = new BankRequest();
            var bankResponse = new BankResponse { Authorized = true, AuthorizationCode = "386a3762-7ebc-4820-bd9c-99d4056584c4" };
            var payment = CreateTestPayment(request, bankResponse);
            var response = new PostPaymentResponse { Id = payment.Id, Status = PaymentStatusDto.Authorized };

            _mockValidator.Setup(v => v.ValidateAsync(request, CancellationToken.None))
                .ReturnsAsync(new ValidationResult());

            _mockMapper.Setup(m => m.Map<BankRequest>(request))
                .Returns(bankRequest);

            _mockAcquiringBank.Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), CancellationToken.None))
                .ReturnsAsync(bankResponse);

            _mockMapper.Setup(m => m.Map<PostPaymentResponse>(It.IsAny<Payment>()))
                .Returns(response);

            _mockPaymentRepository.Setup(p => p.SavePaymentAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            _mockIdempotencyService.Setup(x => x.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, idempotencyKey))
                .ReturnsAsync((null, ComputeHash(request)));
            
            _mockIdempotencyService
                .Setup(x => x.SaveResponseAsync(
                    It.Is<string>(key => key == idempotencyKey),
                    It.Is<PostPaymentResponse>(resp => resp == response),
                    It.IsAny<string>(),           
                    It.IsAny<TimeSpan?>()         
                ))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _handler.HandleAsync(request, idempotencyKey);

            // Assert
            result.Should().BeEquivalentTo(response);
            _mockIdempotencyService.Verify(x => x.SaveResponseAsync(
                It.Is<string>(key => key == idempotencyKey),
                It.Is<PostPaymentResponse>(resp => resp == response),
                It.IsAny<string>(),      
                It.IsAny<TimeSpan?>()   
            ), Times.Once);
            _mockPaymentRepository.Verify(p => p.SavePaymentAsync(It.Is<Payment>(x => x.Status == PaymentStatus.Authorized)), Times.Once);
        }

        [Fact(DisplayName = "HandleAsync works when idempotency key is null or empty")]
        public async Task HandleAsync_Works_WhenIdempotencyKeyIsNullOrEmpty()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            var bankRequest = new BankRequest();
            var bankResponse = new BankResponse { Authorized = true, AuthorizationCode = "83a37c39-c171-4716-b15c-7dbcbd7b457b" };
            var payment = CreateTestPayment(request, bankResponse);

            _mockValidator.Setup(v => v.ValidateAsync(request, CancellationToken.None))
                .ReturnsAsync(new ValidationResult());

            _mockMapper.Setup(m => m.Map<BankRequest>(request))
                .Returns(bankRequest);

            _mockAcquiringBank.Setup(b => b.AuthorizePaymentAsync(bankRequest, CancellationToken.None))
                .ReturnsAsync(bankResponse);

            _mockMapper.Setup(m => m.Map<PostPaymentResponse>(It.IsAny<Payment>()))
                .Returns(new PostPaymentResponse { Id = payment.Id, Status = PaymentStatusDto.Authorized });

            _mockPaymentRepository.Setup(p => p.SavePaymentAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            // Act
            var resultNullKey = await _handler.HandleAsync(request, null!);
            var resultEmptyKey = await _handler.HandleAsync(request, "");

            // Assert
            resultNullKey.Status.Should().Be(PaymentStatusDto.Authorized);
            resultEmptyKey.Status.Should().Be(PaymentStatusDto.Authorized);
            _mockPaymentRepository.Verify(p => p.SavePaymentAsync(It.IsAny<Payment>()), Times.Exactly(2));
        }

        [Fact(DisplayName = "HandleAsync works correctly when idempotency key is whitespace")]
        public async Task HandleAsync_Works_WhenIdempotencyKeyIsWhitespace()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            var whitespaceKey = "   ";
            var bankRequest = new BankRequest();
            var bankResponse = new BankResponse { Authorized = true, AuthorizationCode = "420d41a9-07eb-4e1d-a308-c3a010f82626" };

            _mockValidator.Setup(v => v.ValidateAsync(request, CancellationToken.None))
                .ReturnsAsync(new ValidationResult());

            _mockMapper.Setup(m => m.Map<BankRequest>(request))
                .Returns(bankRequest);

            _mockAcquiringBank.Setup(b => b.AuthorizePaymentAsync(bankRequest, CancellationToken.None))
                .ReturnsAsync(bankResponse);

            _mockMapper.Setup(m => m.Map<PostPaymentResponse>(It.IsAny<Payment>()))
                .Returns(new PostPaymentResponse { Id = Guid.NewGuid(), Status = PaymentStatusDto.Authorized });

            _mockPaymentRepository.Setup(p => p.SavePaymentAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(request, whitespaceKey);

            // Assert
            result.Status.Should().Be(PaymentStatusDto.Authorized);
            _mockPaymentRepository.Verify(p => p.SavePaymentAsync(It.IsAny<Payment>()), Times.Once);
        }

        [Fact(DisplayName = "HandleAsync bubbles up unexpected exceptions from idempotency service")]
        public async Task HandleAsync_BubblesUpUnexpectedExceptions_FromIdempotencyService()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            const string idempotencyKey = "3be1ed0f-5505-4c28-b920-ef79eb2b6ea1";

            _mockIdempotencyService
                .Setup(s => s.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(request, idempotencyKey))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            Func<Task> act = () => _handler.HandleAsync(request, idempotencyKey);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Unexpected error");
        }

        #endregion

        #region Defensive Tests for External Dependencies

        [Fact(DisplayName = "HandleAsync throws when bank returns null response")]
        public async Task HandleAsync_ShouldThrow_WhenBankReturnsNull()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();

            _mockValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockMapper.Setup(m => m.Map<BankRequest>(It.IsAny<PostPaymentRequest>()))
                .Returns(new BankRequest());

            _mockAcquiringBank.Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BankResponse)null!); // Simulate null response

            // Act
            Func<Task> act = async () => await _handler.HandleAsync(request, null!);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Acquiring bank returned an unexpected null response.");
        }

        [Fact(DisplayName = "HandleAsync bubbles exceptions from payment repository")]
        public async Task HandleAsync_BubblesException_WhenSavePaymentFails()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            var bankRequest = new BankRequest();
            var bankResponse = new BankResponse { Authorized = true, AuthorizationCode = "38a9d424-37e1-4c42-8924-067762f753fd" };

            _mockValidator.Setup(v => v.ValidateAsync(request, CancellationToken.None))
                .ReturnsAsync(new ValidationResult());

            _mockMapper.Setup(m => m.Map<BankRequest>(request))
                .Returns(bankRequest);

            _mockAcquiringBank.Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), CancellationToken.None))
                .ReturnsAsync(bankResponse);

            _mockPaymentRepository.Setup(p => p.SavePaymentAsync(It.IsAny<Payment>()))
                .ThrowsAsync(new Exception("DB failure"));

            // Act
            Func<Task> act = () => _handler.HandleAsync(request, null!);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("DB failure");
        }

        #endregion

        #region Failure Scenarios

        [Fact(DisplayName = "HandleAsync throws InvalidOperationException if mapper returns null")]
        public async Task HandleAsync_Throws_WhenMapperReturnsNull()
        {
            // Arrange
            var request = CreateValidPostPaymentRequest();
            
            _mockIdempotencyService
                .Setup(x => x.TryGetCachedResponseAsync<PostPaymentRequest, PostPaymentResponse>(It.IsAny<PostPaymentRequest>(), It.IsAny<string?>()!))
                .ReturnsAsync((null, "hash"));
            
            _mockValidator
                .Setup(v => v.ValidateAsync(request, CancellationToken.None))
                .ReturnsAsync(new ValidationResult());
            
            _mockAcquiringBank
                .Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BankResponse { Authorized = true, AuthorizationCode = "0c81bb41-7f0f-405e-a1a4-5534e237dff6" });

            _mockMapper
                .Setup(m => m.Map<BankRequest>(request))
                .Returns(new BankRequest());
           
            _mockPaymentRepository
                .Setup(r => r.SavePaymentAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);
            
            _mockMapper
                .Setup(m => m.Map<PostPaymentResponse>(It.IsAny<Payment>()))
                .Returns(((PostPaymentResponse?)null)!);

            // Act
            Task Act() => _handler.HandleAsync(request, null!);

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(Act);
            exception.Message.Should().StartWith("Mapping to PostPaymentResponse failed for payment ID ");

            _mockLogger.Verify(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mapping to PostPaymentResponse failed")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        #endregion

        #region Helpers

        private static PostPaymentRequest CreateValidPostPaymentRequest() =>
            new()
            {
                CardNumber = "4000056655665556",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Currency = "GBP",
                Amount = 1000,
                Cvv = "123"
            };

        private static Payment CreateTestPayment(PostPaymentRequest request, BankResponse bankResponse) =>
            new(
                request.CardNumber,
                request.ExpiryMonth,
                request.ExpiryYear,
                Enum.Parse<Currency>(request.Currency),
                request.Amount,
                request.Cvv,
                bankResponse.AuthorizationCode,
                bankResponse.Authorized
                    ? PaymentStatus.Authorized
                    : PaymentStatus.Declined);

        private static string ComputeHash<T>(T obj)
        {
            // Simplified deterministic hash for testing:
            // You may want to replace this with a proper hashing algorithm like SHA256 for production.
            return obj?.ToString()?.GetHashCode().ToString() ?? string.Empty;
        }

        #endregion
    }
}