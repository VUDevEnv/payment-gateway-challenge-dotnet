namespace PaymentGateway.Domain.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when a requested resource is not found.
    /// This exception is typically used when an entity or data item cannot be found 
    /// during operations like retrieval or access.
    /// </summary>
    [Serializable]
    public sealed class NotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specific error message.
        /// This constructor allows you to provide a custom error message that describes the cause of the exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <remarks>
        /// This exception should be thrown when an entity or resource that is expected to exist is not found,
        /// such as when a database lookup fails or a required resource is missing.
        /// </remarks>
        public NotFoundException(string message) : base(message)
        {
        }
    }
}