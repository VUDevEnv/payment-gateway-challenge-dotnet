using System.Diagnostics;
using Asp.Versioning;
using ApiVersioningOptions = PaymentGateway.Api.Options.ApiVersioningOptions;

namespace PaymentGateway.Api.Extensions
{
    public static class ApiVersioningServiceExtensions
    {
        /// <summary>
        /// Configures and adds API versioning services to the application's dependency injection container.
        /// </summary>
        /// <param name="services">The collection of services to which the API versioning services will be added.</param>
        /// <param name="configuration">The application's configuration, which is used to load API versioning options.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> with API versioning services registered.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the 'ApiVersioning' section is missing from the configuration.</exception>
        public static IServiceCollection AddApiVersioningService(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("ApiVersioning");
           
            if (!section.Exists())
                throw new InvalidOperationException("Missing 'ApiVersioning' section in configuration.");
          
            var versioningOptions = new ApiVersioningOptions();
            section.Bind(versioningOptions);

            // Attempt to parse the default API version from the configuration.
            var parsedDefaultVersion = Version.TryParse(versioningOptions.DefaultVersion, out var v)
                ? new ApiVersion(v.Major, v.Minor)
                : ApiVersion.Default;

            // Log a warning if the default API version format is invalid.
            if (parsedDefaultVersion == ApiVersion.Default)
            {
                Debug.WriteLine("Invalid API version format in configuration. Falling back to default.");
            }

            // Add the API versioning services with the options specified in the configuration.
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = parsedDefaultVersion; // Set the default API version.
                options.ReportApiVersions = versioningOptions.ReportApiVersions; // Configure whether to report API versions in response headers.
                options.AssumeDefaultVersionWhenUnspecified = versioningOptions.AssumeDefaultVersionWhenUnspecified; // Decide if the default version should be assumed for unspecified API versions.
                options.UnsupportedApiVersionStatusCode = versioningOptions.UnsupportedStatusCode; // Set the HTTP status code for unsupported API versions.

                var readers = new List<IApiVersionReader>(); 

                // Add the appropriate API version readers based on configuration.
                if (versioningOptions.UseHeader)
                    readers.Add(new HeaderApiVersionReader(versioningOptions.HeaderName));

                if (versioningOptions.UseUrlSegment)
                    readers.Add(new UrlSegmentApiVersionReader());

                if (versioningOptions.UseQueryString)
                    readers.Add(new QueryStringApiVersionReader("api-version"));

                // Log a warning if no API version readers are configured.
                if (readers.Count == 0)
                {
                    Debug.WriteLine("Warning: No API version readers configured. Defaulting to HeaderApiVersionReader.");
                }

                // Combine the configured readers, or use a fallback if no readers are configured.
                options.ApiVersionReader = readers.Count > 0
                    ? ApiVersionReader.Combine(readers.ToArray())
                    : new HeaderApiVersionReader(versioningOptions.HeaderName); // Safe fallback
            })
            // Configure API Explorer options to enable versioned API documentation.
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV"; // Format version in API Explorer.
                options.SubstituteApiVersionInUrl = true; // Automatically substitute API version in URLs for versioned endpoints.
            });

            return services; 
        }
    }
}
