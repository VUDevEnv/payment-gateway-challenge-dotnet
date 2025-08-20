using System.Net;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentGateway.Domain.Interfaces.AcquiringBank;
using PaymentGateway.Domain.Models;
using PaymentGateway.Infrastructure.Options;
using Polly;
using Polly.Timeout;

namespace PaymentGateway.Infrastructure.ExternalServices
{
    /// <summary>
    /// Simulates communication with an acquiring bank for payment authorization.
    /// Implements resilience policies (timeout, retry, circuit breaker) for robust communication.
    /// </summary>
    public class SimulatedAcquiringBank : IAcquiringBank
    {
        private readonly HttpClient _httpClient;
        private readonly IMapper _mapper;
        private readonly ILogger<SimulatedAcquiringBank> _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly string _paymentEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedAcquiringBank"/> class.
        /// Sets up resilience policies for handling external communication reliability.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making requests to the acquiring bank.</param>
        /// <param name="mapper">The AutoMapper instance for mapping objects between layers.</param>
        /// <param name="logger">The logger for logging activities within the acquiring bank service.</param>
        /// <param name="retryOptions">The retry policy options.</param>
        /// <param name="acquiringBankOptions">The acquiring bank configuration options (e.g., payment endpoint).</param>
        public SimulatedAcquiringBank(
            HttpClient httpClient,
            IMapper mapper,
            ILogger<SimulatedAcquiringBank> logger,
            IOptions<RetryPolicyOptions> retryOptions,
            IOptions<AcquiringBankOptions> acquiringBankOptions)
        {
            _httpClient = httpClient;
            _mapper = mapper;
            _logger = logger;
            _paymentEndpoint = acquiringBankOptions.Value.PaymentEndpoint;

            var options = retryOptions.Value;

            // Setup timeout, retry, and circuit breaker policies to manage external service reliability
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(options.TimeoutSeconds), TimeoutStrategy.Pessimistic);
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(response => response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                .WaitAndRetryAsync(
                    retryCount: options.RetryCount,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, _) =>
                    {
                        _logger.LogWarning("Retry {RetryAttempt} after {DelaySeconds}s due to {Error}",
                            retryAttempt, timespan.TotalSeconds, outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
                    });

            var circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(response => response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: options.CircuitBreaker.FailureThreshold,
                    durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
                    onBreak: (outcome, breakDelay) =>
                    {
                        _logger.LogWarning("Circuit broken for {Delay}s due to {Error}",
                            breakDelay.TotalSeconds, outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
                    },
                    onReset: () => _logger.LogInformation("Circuit reset."),
                    onHalfOpen: () => _logger.LogInformation("Circuit half-open."));

            _resiliencePolicy = Policy.WrapAsync(timeoutPolicy, circuitBreakerPolicy, retryPolicy);
        }

        /// <summary>
        /// Authorizes a payment request with the acquiring bank.
        /// Applies resilience policies (retry, timeout, circuit breaker) during the HTTP request.
        /// </summary>
        /// <param name="bankRequest">The payment authorization request to send to the acquiring bank.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
        /// <returns>A <see cref="Task{BankResponse}"/> representing the asynchronous operation, with the response from the acquiring bank.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if deserialization of the acquiring bank response fails or acquiring bank response is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if mapping from the acquiring bank response to <see cref="BankResponse"/> fails.
        /// </exception>
        public async Task<BankResponse> AuthorizePaymentAsync(
            BankRequest bankRequest,
            CancellationToken cancellationToken = default)
        {
            // Map the payment request to the acquiring bank's request model
            var acquiringBankRequest = _mapper.Map<AcquiringBankRequest>(bankRequest);
            var requestJson = JsonSerializer.Serialize(acquiringBankRequest, JsonOptions);

            // Execute the resilience policies (retry, timeout, circuit breaker) during the HTTP request
            var httpResponse = await _resiliencePolicy.ExecuteAsync(async ct =>
            {
                var httpRequest = CreateHttpRequest(requestJson);
                var response = await _httpClient.SendAsync(httpRequest, ct);
                // Ensure success to propagate any failure correctly
                response.EnsureSuccessStatusCode();
                return response;
            }, cancellationToken);

            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            AcquiringBankResponse? acquiringBankResponse;
            try
            {
                acquiringBankResponse = JsonSerializer.Deserialize<AcquiringBankResponse>(responseContent, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize acquiring bank response: {Response}", responseContent);
                throw new InvalidOperationException("Invalid response from acquiring bank. The response could not be deserialized.", ex);
            }

            if (acquiringBankResponse is null)
            {
                _logger.LogError("Failed to deserialize response: {Response}", responseContent);
                throw new InvalidOperationException("Invalid response from acquiring bank. The deserialized response is null.");
            }

            _logger.LogInformation("Payment authorized: Authorized={Authorized}, AuthorizationCode={Code}",
                acquiringBankResponse.Authorized,
                acquiringBankResponse.AuthorizationCode);

            // Map the acquiring bank response to a bank response
            var bankResponse = _mapper.Map<BankResponse>(acquiringBankResponse);

            if (bankResponse is null)
            {
                _logger.LogError("Mapping to BankResponse failed for acquiring bank response: {AcquiringBankResponse}", acquiringBankResponse);
                // Throwing exception if mapping fails
                throw new InvalidOperationException("Could not map acquiring bank response to BankResponse. The mapping returned null.");
            }

            return bankResponse;
        }

        /// <summary>
        /// Creates an HTTP request to the acquiring bank's payment endpoint with the specified request JSON.
        /// </summary>
        /// <param name="requestJson">The serialized request to send to the acquiring bank.</param>
        /// <returns>A <see cref="HttpRequestMessage"/> configured with the payment endpoint and request content.</returns>
        private HttpRequestMessage CreateHttpRequest(string requestJson)
        {
            return new HttpRequestMessage(HttpMethod.Post, _paymentEndpoint)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
