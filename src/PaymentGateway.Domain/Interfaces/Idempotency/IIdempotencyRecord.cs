namespace PaymentGateway.Domain.Interfaces.Idempotency
{
    /// <summary>
    /// Represents an idempotency record for tracking unique requests and their associated responses.
    /// This interface is typically used to ensure that repeated requests with the same key do not result in
    /// duplicate processing, especially in cases like payment retries.
    /// </summary>
    public interface IIdempotencyRecord
    {
        /// <summary>
        /// Gets the unique identifier (key) for the idempotency record, typically used to identify requests.
        /// This key ensures that repeated requests with the same key are not processed multiple times.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets the request hash that uniquely identifies the request's content. This is typically a hashed
        /// version of the request body to ensure that the exact content of the request is the same if retried.
        /// </summary>
        string RequestHash { get; }

        /// <summary>
        /// Gets the date and time when this idempotency record was created.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the optional expiration date and time of the idempotency record.
        /// If an expiration date is set, the record will no longer be valid after this time.
        /// </summary>        
        DateTime? ExpiresAt { get; }
    }
}
