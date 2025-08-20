namespace PaymentGateway.Application.Interfaces
{
    public interface IIdempotencyService
    {
        /// <summary>
        /// Attempts to retrieve a previously cached response using the given idempotency key.
        /// If found, verifies the request hash to ensure payload consistency.
        /// </summary>
        /// <typeparam name="TRequest">The type of the original request.</typeparam>
        /// <typeparam name="TResponse">The type of the cached response.</typeparam>
        /// <param name="request">The incoming request object.</param>
        /// <param name="idempotencyKey">The idempotency key from the client.</param>
        /// <returns>
        /// A tuple containing the cached response (or <c>null</c>) and the computed request hash.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="request"/> is <c>null</c> or <paramref name="idempotencyKey"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the idempotency key has been reused with a different payload.</exception>
        Task<(TResponse? Response, string? RequestHash)> TryGetCachedResponseAsync<TRequest, TResponse>(
            TRequest request,
            string idempotencyKey);

        /// <summary>
        /// Stores a response for future retrieval under the specified idempotency key.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="idempotencyKey">The idempotency key.</param>
        /// <param name="response">The response to cache.</param>
        /// <param name="requestHash">The hash of the request used for matching on retry.</param>
        /// <param name="ttl">The optional time-to-live (TTL) after which the cached response should expire.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="idempotencyKey"/> or <paramref name="requestHash"/> is <c>null</c> or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="response"/> is <c>null</c>.</exception>
        Task SaveResponseAsync<TResponse>(
            string idempotencyKey,
            TResponse response,
            string requestHash,
            TimeSpan? ttl = null);
    }
}
