namespace PaymentGateway.Infrastructure.Options
{
    /// <summary>
    /// Represents configuration options for the circuit breaker mechanism.
    /// These options control how the circuit breaker reacts to failures and determines when to "break" and when to "reset".
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Gets or sets the failure threshold for triggering the circuit breaker.
        /// </summary>
        /// <remarks>
        /// This value specifies the number of consecutive failures that must occur before the circuit breaker is tripped.
        /// For example, if set to 5, the circuit breaker will be triggered after 5 consecutive failures.
        /// </remarks>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the duration in seconds that the circuit breaker stays open before attempting to reset.
        /// </summary>
        /// <remarks>
        /// This value specifies how long the circuit breaker remains in an "open" state after reaching the failure threshold.
        /// During this time, requests will be rejected or redirected to a fallback. After this duration, the circuit breaker will attempt to close and resume normal operations.
        /// </remarks>
        public int BreakDurationSeconds { get; set; } = 60;
    }
}