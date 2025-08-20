using PaymentGateway.Domain.Interfaces.Idempotency;

namespace PaymentGateway.Domain.Entities
{
    /// <summary>
    /// Represents a record used for ensuring idempotency in payment operations. 
    /// An Idempotency Record allows a payment request to be retried without risk of duplicating the transaction.
    /// This helps avoid processing the same payment multiple times.
    /// </summary>
    public class IdempotencyRecord<T> : IIdempotencyRecord
    {
        /// <summary>
        /// Gets or sets the unique key used to identify the idempotency record.
        /// This key is typically tied to the original payment request, ensuring that retries are safely handled.
        /// </summary>
        /// <remarks>
        /// The key is used to track the request across different retries and ensure that the same request
        /// is not processed more than once. It is typically generated at the start of the payment process.
        /// </remarks>
        public string Key { get; set; } = null!;

        /// <summary>
        /// Gets or sets the response associated with the idempotent request.
        /// This could be any type (generic T) and stores the result of the original request for future retries.
        /// </summary>
        /// <remarks>
        /// This property contains the response that was returned from the payment operation and will be returned 
        /// to the client when the same request is retried. It prevents the need to reprocess the payment 
        /// if the same request is repeated.
        /// </remarks>
        public T Response { get; set; } = default!;

        /// <summary>
        /// Gets or sets the hash of the original request used to detect duplicate requests.
        /// This hash ensures the system can recognize identical requests and avoid processing them multiple times.
        /// </summary>
        /// <remarks>
        /// The hash is typically generated from the request body, ensuring that even if the request is retried
        /// with identical data, it is treated as a duplicate and prevented from being processed again.
        /// </remarks>
        public string RequestHash { get; set; } = null!;

        /// <summary>
        /// Gets or sets the timestamp when the idempotency record was created.
        /// This is important for tracking when the request was initially made and can be used for auditing purposes.
        /// </summary>
        /// <remarks>
        /// The timestamp is used to manage expiration logic (if any) and can also help trace the request through logs.
        /// </remarks>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the idempotency record expires.
        /// After this time, new requests with the same key will be treated as new, allowing the request to be processed again.
        /// </summary>
        /// <remarks>
        /// The expiration date ensures that idempotency is only maintained for a limited time (e.g., 24 hours).
        /// After expiration, the same key can be reused for future requests.
        /// </remarks>
        public DateTime? ExpiresAt { get; set; }
    }
}
