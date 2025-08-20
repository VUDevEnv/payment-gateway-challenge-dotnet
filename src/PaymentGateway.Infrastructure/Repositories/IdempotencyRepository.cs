using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Interfaces.Idempotency;
using PaymentGateway.Domain.Interfaces.Repositories;

namespace PaymentGateway.Infrastructure.Repositories
{
    public class IdempotencyRepository(ILogger<IdempotencyRepository> logger) : IIdempotencyRepository
    {
        private readonly ConcurrentDictionary<string, IIdempotencyRecord> _store = new();

        /// <summary>
        /// Retrieves an idempotency record for a given key, if it exists and is not expired.
        /// </summary>
        /// <typeparam name="T">The type of the response associated with the idempotency record.</typeparam>
        /// <param name="key">The unique identifier for the idempotency record.</param>
        /// <returns>A task representing the asynchronous operation, with the idempotency record if found and valid, otherwise <c>null</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="key"/> is null or whitespace.</exception>
        public Task<IdempotencyRecord<T>?> GetRecordAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

            if (_store.TryGetValue(key, out var recordObj) && recordObj is IdempotencyRecord<T> record)
            {
                var now = DateTime.UtcNow;
                // Remove the record if it has expired
                if (record.ExpiresAt != null && now > record.ExpiresAt.Value)
                {
                    _store.TryRemove(key, out _);
                    logger.LogInformation("Idempotency record expired and removed for key: {Key}", key);

                    return Task.FromResult<IdempotencyRecord<T>?>(null);
                }

                return Task.FromResult<IdempotencyRecord<T>?>(record);
            }

            return Task.FromResult<IdempotencyRecord<T>?>(null);
        }

        /// <summary>
        /// Saves a new idempotency record or updates an existing one for a given key.
        /// </summary>
        /// <typeparam name="T">The type of the response to be stored in the idempotency record.</typeparam>
        /// <param name="key">The unique identifier for the idempotency record.</param>
        /// <param name="response">The response associated with the idempotency record.</param>
        /// <param name="requestHash">The hash of the request to track uniqueness.</param>
        /// <param name="ttl">The optional time-to-live for the idempotency record. If not provided, the record does not expire.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="key"/> or <paramref name="requestHash"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="response"/> is null.</exception>
        public Task SaveRecordAsync<T>(string key, T response, string requestHash, TimeSpan? ttl = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (string.IsNullOrEmpty(requestHash))
                throw new ArgumentException("Request hash cannot be null or empty.", nameof(requestHash));

            var record = new IdempotencyRecord<T>
            {
                Key = key,
                Response = response,
                RequestHash = requestHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
            };

            // Add or update the record atomically
            _store.AddOrUpdate(key, record, (_, _) => record);

            return Task.CompletedTask;
        }
    }
}
