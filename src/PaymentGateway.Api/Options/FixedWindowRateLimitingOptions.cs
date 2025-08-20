using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Options
{
    /// <summary>
    /// Represents configuration options for rate limiting using the Fixed Window algorithm.
    /// These options control the rate limit parameters such as the number of requests allowed within a time window
    /// and the rejection status code when the limit is exceeded.
    /// </summary>
    public class FixedWindowRateLimitingOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of requests allowed within the time window.
        /// </summary>
        /// <remarks>
        /// This value sets the permit limit, which determines how many requests a client can make within a specified time window.
        /// It must be a positive integer.
        /// </remarks>
        [Range(1, int.MaxValue, ErrorMessage = "Permit limit must be a positive integer.")]
        public int PermitLimit { get; set; }

        /// <summary>
        /// Gets or sets the duration (in seconds) of the time window for rate limiting.
        /// </summary>
        /// <remarks>
        /// This value defines the window period (in seconds) during which the request count is tracked.
        /// It must be between 1 second and 3600 seconds (1 hour).
        /// </remarks>
        [Range(1, 3600, ErrorMessage = "Window seconds must be between 1 and 3600 seconds.")]
        public int WindowSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of requests that can be queued for processing after the limit is exceeded.
        /// </summary>
        /// <remarks>
        /// This value controls how many requests can be queued when the rate limit is exceeded. Requests that exceed this
        /// queue limit will be rejected immediately. It must be a non-negative integer.
        /// </remarks>
        [Range(0, int.MaxValue, ErrorMessage = "Queue limit must be a non-negative integer.")]
        public int QueueLimit { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code to return when a request is rejected due to rate limiting.
        /// </summary>
        /// <remarks>
        /// This value determines the status code sent in the response when the rate limit has been exceeded.
        /// The default value is <c>429 Too Many Requests</c>, but it can be customized.
        /// </remarks>
        public int RejectionStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;
    }
}
