namespace PaymentGateway.Infrastructure.Options
{
    /// <summary>
    /// Represents configuration options for the retry policy.
    /// These options define how the system should retry failed operations, including the number of retries,
    /// timeout duration, and circuit breaker settings.
    /// </summary>
    public class RetryPolicyOptions
    {
        /// <summary>
        /// Gets or sets the number of retry attempts to make before failing the operation.
        /// </summary>
        /// <remarks>
        /// This value specifies how many times the operation will be retried in the event of a failure.
        /// For example, if set to 3, the system will try the operation 3 times before giving up.
        /// </remarks>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the timeout duration in seconds for each retry attempt.
        /// </summary>
        /// <remarks>
        /// This value specifies how long the system should wait for a response before considering the retry attempt to have failed.
        /// For example, if set to 30, the system will wait up to 30 seconds before giving up on each retry.
        /// </remarks>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the configuration options for the circuit breaker.
        /// </summary>
        /// <remarks>
        /// The circuit breaker is used to prevent overloading the system with retries when failures persist.
        /// If the failure threshold is reached, the circuit breaker will open and prevent further retries for a specified duration.
        /// </remarks>
        public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    }
}