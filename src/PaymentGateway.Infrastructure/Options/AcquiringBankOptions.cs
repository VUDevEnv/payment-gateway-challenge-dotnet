namespace PaymentGateway.Infrastructure.Options
{
    /// <summary>
    /// Represents configuration options for interacting with the acquiring bank.
    /// These options define the base URL for the bank's API and the specific endpoint for payment transactions.
    /// </summary>
    public class AcquiringBankOptions
    {
        /// <summary>
        /// Gets or sets the base URL of the acquiring bank's API.
        /// </summary>
        /// <remarks>
        /// This URL is the root address for all API requests made to the acquiring bank's payment gateway.
        /// It typically includes the protocol (http or https) and the domain, e.g., "https://api.bank.com".
        /// </remarks>
        public string BaseUrl { get; set; } = null!;

        /// <summary>
        /// Gets or sets the endpoint used for processing payment transactions.
        /// </summary>
        /// <remarks>
        /// The default value is "payments", but this can be customized if the acquiring bank's API uses a 
        /// different endpoint for payments. This value is appended to the <see cref="BaseUrl"/> to form the 
        /// full URL for payment requests.
        /// </remarks>
        public string PaymentEndpoint { get; set; } = "payments";
    }
}