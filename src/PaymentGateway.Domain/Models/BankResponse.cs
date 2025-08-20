namespace PaymentGateway.Domain.Models
{
    /// <summary>
    /// Represents the response received from the bank after a transaction request.
    /// This model contains the results of the authorization process.
    /// </summary>
    public class BankResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the payment was successfully authorized by the bank.
        /// </summary>
        /// <remarks>
        /// If this value is true, it means the payment was approved by the bank and the transaction is complete.
        /// If false, the payment was not authorized, and the transaction was either declined or failed.
        /// </remarks>
        public bool Authorized { get; set; }

        /// <summary>
        /// Gets or sets the authorization code returned by the bank, if the payment was authorized.
        /// </summary>
        /// <remarks>
        /// The authorization code is a unique identifier issued by the bank to track and confirm that the payment was successfully processed.
        /// This code can be used for transaction tracking, status inquiries, or initiating future actions like refunds.
        /// </remarks>
        public string AuthorizationCode { get; set; } = null!;
    }
}