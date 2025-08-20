namespace PaymentGateway.Application.Constants
{
    /// <summary>
    /// Contains the validation messages used across the application.
    /// These messages are intended to provide consistent error feedback during input validation.
    /// </summary>
    public static class ValidationMessages
    {
        // CardNumber
        /// <summary>
        /// Error message indicating that the card number is required.
        /// </summary>
        public const string CardNumberRequired = "Card number is required.";

        /// <summary>
        /// Error message indicating that the card number must be between 14 and 19 digits long.
        /// </summary>
        public const string CardNumberLength = "Card number must be between 14 and 19 digits long.";

        /// <summary>
        /// Error message indicating that the card number must only contain numeric digits.
        /// </summary>
        public const string CardNumberNumeric = "Card number must contain only numeric digits.";

        // ExpiryMonth
        /// <summary>
        /// Error message indicating that the expiry month is required.
        /// </summary>
        public const string ExpiryMonthRequired = "Expiry month is required.";

        /// <summary>
        /// Error message indicating that the expiry month must be between 1 and 12.
        /// </summary>
        public const string ExpiryMonthOutOfRange = "Expiry month must be between 1 and 12.";

        // ExpiryYear
        /// <summary>
        /// Error message indicating that the expiry year is required.
        /// </summary>
        public const string ExpiryYearRequired = "Expiry year is required.";

        /// <summary>
        /// Error message indicating that the expiry year must be the current year or later.
        /// </summary>
        public const string ExpiryYearTooEarly = "Expiry year must be the current year or later.";

        /// <summary>
        /// Error message indicating that the expiry date must be in the future.
        /// </summary>
        public const string ExpiryDateInPast = "Expiry date must be in the future.";

        // Currency
        /// <summary>
        /// Error message indicating that the currency is required.
        /// </summary>
        public const string CurrencyRequired = "Currency is required.";

        /// <summary>
        /// Error message indicating that the currency must be a 3-letter ISO code.
        /// </summary>
        public const string CurrencyInvalidLength = "Currency must be a 3-letter ISO code.";

        /// <summary>
        /// Error message indicating that the provided currency is not supported.
        /// This message dynamically includes the list of allowed currencies.
        /// </summary>
        /// <param name="allowedCurrencies">A collection of allowed currencies.</param>
        /// <returns>A formatted string indicating which currencies are supported.</returns>
        public static string CurrencyNotSupported(IEnumerable<string> allowedCurrencies) =>
            $"Currency must be one of the following: {string.Join(", ", allowedCurrencies.OrderBy(c => c))}.";

        // Amount
        /// <summary>
        /// Error message indicating that the amount is required.
        /// </summary>
        public const string AmountRequired = "Amount is required.";

        /// <summary>
        /// Error message indicating that the amount must be greater than zero.
        /// </summary>
        public const string AmountMustBeGreaterThanZero = "Amount must be greater than zero.";

        // CVV
        /// <summary>
        /// Error message indicating that the CVV is required.
        /// </summary>
        public const string CvvRequired = "CVV is required.";

        /// <summary>
        /// Error message indicating that the CVV must be 3 or 4 digits long.
        /// </summary>
        public const string CvvLengthInvalid = "CVV must be 3 or 4 digits long.";

        /// <summary>
        /// Error message indicating that the CVV must contain only numeric digits.
        /// </summary>
        public const string CvvNotNumeric = "CVV must contain only numeric digits.";
    }
}
