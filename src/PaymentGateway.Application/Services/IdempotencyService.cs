using System.Globalization;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Utilities;
using PaymentGateway.Domain.Interfaces.Repositories;

namespace PaymentGateway.Application.Services
{
    public class IdempotencyService(
        IIdempotencyRepository idempotencyRepository,
        ILogger<IdempotencyService> logger)
        : IIdempotencyService
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
        public async Task<(TResponse? Response, string? RequestHash)> TryGetCachedResponseAsync<TRequest, TResponse>(
            TRequest request,
            string idempotencyKey)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentNullException(nameof(idempotencyKey));

            var requestHash = RequestHasher.ComputeHash(request);

            var existingRecord = await idempotencyRepository.GetRecordAsync<TResponse>(idempotencyKey);
            if (existingRecord != null)
            {
                if (existingRecord.RequestHash != requestHash)
                {
                    logger.LogWarning(
                        "Idempotency-Key '{Key}' reuse with different payloads. Stored hash: {StoredHash}, Current hash: {CurrentHash}",
                        idempotencyKey, existingRecord.RequestHash, requestHash);

                    throw new InvalidOperationException("Idempotency-Key has already been used with a different request payload.");
                }

                logger.LogInformation("Returning cached response for Idempotency-Key: {Key}", idempotencyKey);
                return (existingRecord.Response, requestHash);
            }

            return (default, requestHash);
        }
        
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
        public Task SaveResponseAsync<TResponse>(
            string idempotencyKey,
            TResponse response,
            string requestHash,
            TimeSpan? ttl = null)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("The idempotency key must not be null or whitespace.", nameof(idempotencyKey));
            if (string.IsNullOrWhiteSpace(requestHash))
                throw new ArgumentException("The request hash must not be null or whitespace.", nameof(requestHash));
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            logger.LogInformation(
                "Saving idempotency response with key: {Key}, TTL: {TTL}",
                idempotencyKey, ttl?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? "None"
            );

            return idempotencyRepository.SaveRecordAsync(idempotencyKey, response, requestHash, ttl);
        }
    }
}
