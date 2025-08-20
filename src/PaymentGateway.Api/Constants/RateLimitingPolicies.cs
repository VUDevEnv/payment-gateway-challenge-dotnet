namespace PaymentGateway.Api.Constants
{
    /// <summary>
    /// Contains the rate-limiting policy constants used across the application.
    /// These constants are used to define different rate-limiting strategies for API requests,
    /// ensuring that clients are prevented from overwhelming the system with too many requests in a short period.
    /// </summary>
    public class RateLimitingPolicies
    {
        /// <summary>
        /// Represents the "FixedWindow" rate-limiting policy.
        /// In this policy, a fixed time window (e.g., 1 minute) is set, and the number of requests is limited within that window.
        /// Once the window is over, the count resets.
        /// </summary>
        public const string FixedWindowPolicy = "FixedWindow";
    }
}