using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.ExternalServices
{
    /// <summary>
    /// Represents a request sent to the acquiring bank or payment processor for a transaction.
    /// This model contains the necessary details to process a payment request.
    /// </summary>
    public class AcquiringBankRequest
    {
        /// <summary>
        /// Gets or sets the full credit card number used for the transaction.
        /// </summary>
        /// <remarks>
        /// This is the 16-digit (or otherwise) card number associated with the payment method being used.
        /// It should be securely provided to ensure it is not exposed to unauthorized parties.
        /// </remarks>
        [JsonPropertyName("card_number")]
        public string CardNumber { get; set; } = null!;

        /// <summary>
        /// Gets or sets the expiration date of the credit card, in the format "MM/YY".
        /// </summary>
        /// <remarks>
        /// This date indicates the month and year when the card expires. It is used to validate the card during processing.
        /// </remarks>
        [JsonPropertyName("expiry_date")]
        public string ExpiryDate { get; set; } = null!;

        /// <summary>
        /// Gets or sets the currency in which the payment is being made, represented as a string (e.g., "GBP", "USD").
        /// </summary>
        /// <remarks>
        /// The currency code should follow the ISO 4217 standard. For example, for Great British Pounds, use "GBP".
        /// This ensures that the payment processor handles the transaction in the correct currency.
        /// </remarks>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = null!;

        /// <summary>
        /// Gets or sets the total amount of the payment in the specified currency, represented in the minor currency unit.
        /// </summary>
        /// <remarks>
        /// The amount is expressed in the smallest unit of the currency. For GBP, this would be pence.
        /// For example, if the payment is for £0.01, the value should be 1 (1 penny).
        /// If the payment is for £10.50, the value should be 1050 (1050 pennies).
        /// </remarks>
        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        /// <summary>
        /// Gets or sets the CVV (Card Verification Value) associated with the card.
        /// </summary>
        /// <remarks>
        /// The CVV is a 3- or 4-digit security code printed on the back (or front, in some cases) of the credit card.
        /// This value is used for additional security during payment processing to ensure that the person making the transaction is the cardholder.
        /// </remarks>
        [JsonPropertyName("cvv")]
        public string Cvv { get; set; } = null!;
    }
}
