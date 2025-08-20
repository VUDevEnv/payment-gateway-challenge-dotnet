namespace PaymentGateway.Api.Options
{
    /// <summary>
    /// Represents configuration options for API versioning.
    /// These options control how the API versioning is handled, including the default version,
    /// version reporting, and how versions are passed in requests.
    /// </summary>
    public class ApiVersioningOptions
    {
        /// <summary>
        /// Gets or sets the default API version to use when no version is specified by the client.
        /// </summary>
        /// <remarks>
        /// This value is used as the fallback version if the client does not provide a version.
        /// The default is set to "1.0".
        /// </remarks>
        public string DefaultVersion { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets a flag indicating whether the API version should be reported in the response headers.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c>, the version information will be included in the response headers
        /// for clients to easily identify the version of the API they are interacting with.
        /// </remarks>
        public bool ReportApiVersions { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag that specifies whether to assume the default version when an API version
        /// is not specified by the client in the request.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c>, the default version will be used when the client does not specify a version.
        /// This ensures backward compatibility for clients not providing version information.
        /// </remarks>
        public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the header used to pass the API version in requests.
        /// </summary>
        /// <remarks>
        /// The default value is "X-Api-Version", but this can be customized to match specific client requirements.
        /// </remarks>
        public string HeaderName { get; set; } = "X-Api-Version";

        /// <summary>
        /// Gets or sets the status code returned when the client requests an unsupported API version.
        /// </summary>
        /// <remarks>
        /// The default status code is <c>406 Not Acceptable</c>, but this can be customized as needed.
        /// </remarks>
        public int UnsupportedStatusCode { get; set; } = StatusCodes.Status406NotAcceptable;

        /// <summary>
        /// Gets or sets a flag that determines whether the API version is specified in the request header.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c>, the version will be read from the request header (e.g., "X-Api-Version").
        /// </remarks>
        public bool UseHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag that determines whether the API version is specified in the URL segment.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c>, the version will be read from the URL path segment (e.g., "/api/v1/resource").
        /// </remarks>
        public bool UseUrlSegment { get; set; } = false;

        /// <summary>
        /// Gets or sets a flag that determines whether the API version is specified in the query string.
        /// </summary>
        /// <remarks>
        /// If set to <c>true</c>, the version will be read from the query string (e.g., "?api-version=1.0").
        /// </remarks>
        public bool UseQueryString { get; set; } = false;
    }
}
