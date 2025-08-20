using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Options;

namespace PaymentGateway.Api.Extensions
{
    public static class RateLimitingServiceExtensions
    {
        /// <summary>
        /// Configures and adds rate-limiting services to the application's dependency injection container.
        /// This method sets up a fixed-window rate limiter using configuration from the application's settings.
        /// </summary>
        /// <param name="services">The collection of services to which rate-limiting services will be added.</param>
        /// <param name="configuration">The application's configuration, used to load rate-limiting options.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> with rate-limiting services registered.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the 'RateLimiting:FixedWindow' section is missing in the configuration.</exception>
        public static IServiceCollection AddRateLimitingService(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("RateLimiting:FixedWindow");
           
            if (!section.Exists())
                throw new InvalidOperationException("Missing 'RateLimiting:FixedWindow' section in configuration.");
           
            var rateLimitOptions = new FixedWindowRateLimitingOptions();
            section.Bind(rateLimitOptions);

            // Add the rate limiter to the service collection with the options specified in the configuration.
            services.AddRateLimiter(options =>
            {
                // Set the status code to return when rate-limiting is triggered (too many requests).
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Add the fixed-window rate limiter policy with specific options for permit limit and window size.
                options.AddFixedWindowLimiter(RateLimitingPolicies.FixedWindowPolicy, limiterOptions =>
                {
                    limiterOptions.PermitLimit = rateLimitOptions.PermitLimit; // The maximum number of requests allowed in the time window.
                    limiterOptions.Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds); // Duration of the rate-limiting window.
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; // Define the order in which requests in the queue will be processed.
                    limiterOptions.QueueLimit = rateLimitOptions.QueueLimit; // The maximum number of requests allowed to wait in the queue.
                });
            });

            return services;
        }
    }
}
