using PaymentGateway.Domain.Models;

namespace PaymentGateway.Domain.Interfaces.AcquiringBank
{
    /// <summary>
    /// Communicates with an acquiring bank to authorize payments.
    /// Implements resilience policies (timeout, retry, circuit breaker) for robust communication.
    /// </summary>
    public interface IAcquiringBank
    {
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
        Task<BankResponse> AuthorizePaymentAsync(
            BankRequest bankRequest,
            CancellationToken cancellationToken = default);
    }
}