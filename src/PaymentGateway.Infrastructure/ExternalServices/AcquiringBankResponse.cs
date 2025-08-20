using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.ExternalServices
{
    /// <summary>
    /// Represents the response received from the acquiring bank after a transaction request.
    /// This model contains the results of the authorization process.
    /// </summary>
    public class AcquiringBankResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the payment was successfully authorized by the bank.
        /// </summary>
        /// <remarks>
        /// If this value is true, it means the payment was approved by the bank and the transaction is complete.
        /// If false, the payment was not authorized, and the transaction was declined or failed.
        /// </remarks>
        [JsonPropertyName("authorized")]
        public bool Authorized { get; set; }

        /// <summary>
        /// Gets or sets the authorization code returned by the bank, if the payment was authorized.
        /// </summary>
        /// <remarks>
        /// The authorization code is a unique identifier assigned by the bank to track and verify the transaction.
        /// This code can be used for future reference, querying the transaction status, or handling subsequent actions like refunds or chargebacks.
        /// </remarks>
        [JsonPropertyName("authorization_code")]
        public string AuthorizationCode { get; set; } = null!;
    }
}