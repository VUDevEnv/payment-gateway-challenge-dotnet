using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Entities
{
    /// <summary>
    /// Represents a payment transaction in the system.
    /// This class holds the details of a payment made by a customer, including credit card information, amount, 
    /// status, and authorization code.
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Payment"/> class with the provided payment details.
        /// </summary>
        /// <param name="cardNumberLastFour">The last four digits of the credit card number used for the transaction.</param>
        /// <param name="expiryMonth">The expiration month of the credit card.</param>
        /// <param name="expiryYear">The expiration year of the credit card.</param>
        /// <param name="currency">The currency used for the payment (e.g., GBP, USD).</param>
        /// <param name="amount">The total payment amount, in the minor currency unit (e.g., pennies for GBP).</param>
        /// <param name="cvv">The CVV (Card Verification Value) associated with the card.</param>
        /// <param name="authorizationCode">The authorization code returned after payment authorization.</param>
        /// <param name="status">The current status of the payment (e.g., Authorized, Declined).</param>
        public Payment(
            string cardNumberLastFour,
            int expiryMonth,
            int expiryYear,
            Currency currency,
            int amount,
            string cvv,
            string authorizationCode,
            PaymentStatus status)
        {
            CardNumberLastFour = cardNumberLastFour;
            ExpiryMonth = expiryMonth;
            ExpiryYear = expiryYear;
            Currency = currency;
            Amount = amount;
            Cvv = cvv;
            AuthorizationCode = authorizationCode;
            Status = status;
        }

        /// <summary>
        /// Gets the unique identifier for the payment. 
        /// This is automatically generated upon creation of the payment record.
        /// </summary>
        /// <remarks>
        /// The GUID ensures that each payment has a globally unique identifier.
        /// This is useful for tracking and referencing the payment across systems and databases.
        /// </remarks>
        public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// Gets the last four digits of the card number used for the transaction.
        /// This is stored for display purposes, ensuring that sensitive card details are not exposed.
        /// </summary>
        /// <remarks>
        /// This data is typically shown on receipts and is used for user reference, while keeping the full card number secure.
        /// Only the last four digits are stored for security reasons to comply with data protection standards.
        /// </remarks>
        public string CardNumberLastFour { get; private set; }

        /// <summary>
        /// Gets the month in which the card expires.
        /// This is used to validate the card's expiration date during the payment process.
        /// </summary>
        /// <remarks>
        /// The expiry month is part of the card’s expiration date, which is crucial for verifying the validity of the card.
        /// A payment attempt with an expired card will be rejected.
        /// </remarks>
        public int ExpiryMonth { get; private set; }

        /// <summary>
        /// Gets the year in which the card expires.
        /// This is used in combination with the expiry month to verify the card’s validity.
        /// </summary>
        /// <remarks>
        /// The expiry year is also part of the expiration date. It is essential for validating the card during transaction processing.
        /// Both the month and the year together ensure the card has not expired.
        /// </remarks>
        public int ExpiryYear { get; private set; }

        /// <summary>
        /// Gets the currency used for the payment (e.g., GBP, EUR, USD).
        /// This ensures the payment amount is correctly represented in the appropriate currency.
        /// </summary>
        /// <remarks>
        /// The currency is defined by an enumeration (e.g., Currency.GBP for British Pounds).
        /// This ensures that the payment processing system can handle multiple currencies appropriately and apply correct exchange rates when necessary.
        /// </remarks>
        public Currency Currency { get; private set; }

        /// <summary>
        /// Gets the total amount of the payment, represented in the minor currency unit of the specified currency.
        /// This is the amount being charged to the customer, expressed in the smallest unit (e.g., pence for GBP, cents for USD).
        /// </summary>
        /// <remarks>
        /// For example, if the payment is for £0.01 (GBP), the value will be 1 (1 penny).
        /// If the payment is for £10.50 (GBP), the value will be 1050 (1050 pennies).
        /// </remarks>
        public int Amount { get; private set; }

        /// <summary>
        /// Gets or sets the CVV (Card Verification Value) associated with the card.
        /// </summary>
        /// <remarks>
        /// The CVV is a security feature of the card, typically a 3- or 4-digit code found on the back (or front) of the card.
        /// It is used to verify that the person initiating the payment is the legitimate cardholder.
        /// </remarks>
        public string Cvv { get; private set; }

        /// <summary>
        /// Gets the current status of the payment (e.g., Authorized, Declined, Rejected).
        /// This reflects the outcome of the payment authorization process.
        /// </summary>
        /// <remarks>
        /// The status indicates whether the payment was successful or failed. Common statuses include:
        /// - Authorized: The payment was approved.
        /// - Declined: The payment was rejected by the payment processor.
        /// - Rejected: The payment was rejected for reasons such as insufficient funds or invalid details.
        /// </remarks>
        public PaymentStatus Status { get; private set; }

        /// <summary>
        /// Gets the unique authorization code returned by the acquiring bank upon successful payment authorization.
        /// This code is used for reference and tracking the transaction.
        /// </summary>
        /// <remarks>
        /// The authorization code is assigned by the bank or payment gateway to confirm that the payment was authorized.
        /// It is typically used for tracking the payment and handling any potential reversals or disputes.
        /// </remarks>
        public string AuthorizationCode { get; private set; }

        /// <summary>
        /// Gets the timestamp when the payment was created, in UTC.
        /// This is useful for auditing purposes and tracking the payment lifecycle.
        /// </summary>
        /// <remarks>
        /// The timestamp is generated when the payment is created and is used to track when the transaction was initiated.
        /// This information is critical for auditing, reconciliation, and reporting purposes.
        /// </remarks>
        public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
    }
}
