using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Handles interaction with idempotency records. Allows for retrieving and saving idempotency data.
    /// </summary>
    public interface IIdempotencyRepository
    {
        /// <summary>
        /// Retrieves an idempotency record for a given key, if it exists and is not expired.
        /// </summary>
        /// <typeparam name="T">The type of the response associated with the idempotency record.</typeparam>
        /// <param name="key">The unique identifier for the idempotency record.</param>
        /// <returns>A task representing the asynchronous operation, with the idempotency record if found and valid, otherwise <c>null</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="key"/> is null or whitespace.</exception>
        Task<IdempotencyRecord<T>?> GetRecordAsync<T>(string key);

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
        Task SaveRecordAsync<T>(string key, T response, string requestHash, TimeSpan? ttl = null);
    }
}
