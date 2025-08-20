using FluentValidation.TestHelper;
using PaymentGateway.Application.Constants;
using PaymentGateway.Application.Validators;

namespace PaymentGateway.Application.UnitTests.Validators
{
    public class PostPaymentRequestValidatorTests
    {
        private readonly PostPaymentRequestValidator _validator = new();

        #region Valid Cases

        [Fact(DisplayName = "Valid: Should pass validation with all valid fields")]
        public void Should_Pass_Validation_For_Valid_Data()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory(DisplayName = "Valid: Should pass when currency is supported (case-insensitive)")]
        [InlineData("GBP")]
        [InlineData("EUR")]
        [InlineData("USD")]
        [InlineData("gBp")]
        [InlineData("gbp")]
        public void Should_Allow_Supported_Currencies(string currency)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Currency = currency;

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Currency);
        }

        #endregion

        #region Invalid Card Number Cases

        [Fact(DisplayName = "Invalid: Should error when card number is empty")]
        public void Should_Have_Error_When_CardNumber_Is_Empty()
        {
            var request = CreateValidRequest();
            request.CardNumber = "";

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.CardNumber)
                .WithErrorMessage(ValidationMessages.CardNumberRequired);
        }

        [Fact(DisplayName = "Invalid: Should error when card number is too short")]
        public void Should_Have_Error_When_CardNumber_Is_Too_Short()
        {
            var request = CreateValidRequest();
            request.CardNumber = "1234567890123";

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.CardNumber)
                .WithErrorMessage(ValidationMessages.CardNumberLength);
        }

        [Fact(DisplayName = "Invalid: Should error when card number contains letters")]
        public void Should_Have_Error_When_CardNumber_Has_NonDigits()
        {
            var request = CreateValidRequest();
            request.CardNumber = "1234abcd5678efgh";

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.CardNumber)
                .WithErrorMessage(ValidationMessages.CardNumberNumeric);
        }

        #endregion

        #region Invalid ExpiryMonth Cases

        [Fact(DisplayName = "Invalid: Should error when expiry month is empty (0)")]
        public void Should_Have_Error_When_ExpiryMonth_Is_Empty()
        {
            var request = CreateValidRequest();
            request.ExpiryMonth = 0;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth)
                .WithErrorMessage(ValidationMessages.ExpiryMonthRequired);
        }

        [Fact(DisplayName = "Invalid: Should error when expiry month is less than 1")]
        public void Should_Have_Error_When_ExpiryMonth_Is_Less_Than_1()
        {
            var request = CreateValidRequest();
            request.ExpiryMonth = 0;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth)
                .WithErrorMessage(ValidationMessages.ExpiryMonthOutOfRange);
        }

        [Fact(DisplayName = "Invalid: Should error when expiry month is out of range")]
        public void Should_Have_Error_When_ExpiryMonth_Is_Out_Of_Range()
        {
            var request = CreateValidRequest();
            request.ExpiryMonth = 13;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth)
                .WithErrorMessage(ValidationMessages.ExpiryMonthOutOfRange);
        }

        #endregion

        #region Invalid ExpiryYear Cases

        [Fact(DisplayName = "Invalid: Should error when expiry year is empty (0)")]
        public void Should_Have_Error_When_ExpiryYear_Is_Empty()
        {
            var request = CreateValidRequest();
            request.ExpiryYear = 0;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryYear)
                .WithErrorMessage(ValidationMessages.ExpiryYearRequired);
        }

        [Fact(DisplayName = "Invalid: Should error when expiry year is in the past")]
        public void Should_Have_Error_When_ExpiryYear_Is_In_The_Past()
        {
            var request = CreateValidRequest();
            request.ExpiryYear = DateTime.UtcNow.Year - 1;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryYear)
                .WithErrorMessage(ValidationMessages.ExpiryYearTooEarly);
        }

        [Fact(DisplayName = "Invalid: Should error when expiry date (year + month) is in the past")]
        public void Should_Have_Error_When_ExpiryDate_Is_In_The_Past()
        {
            var request = CreateValidRequest();
            var now = DateTime.UtcNow;

            request.ExpiryYear = now.Year;
            request.ExpiryMonth = now.Month - 1 <= 0 ? 12 : now.Month - 1;
            if (request.ExpiryMonth == 12)
                request.ExpiryYear -= 1;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.ExpiryYear)
                .WithErrorMessage(ValidationMessages.ExpiryDateInPast);
        }

        #endregion

        #region Invalid Currency Cases

        [Fact(DisplayName = "Invalid: Should error when currency is empty")]
        public void Should_Have_Error_When_Currency_Is_Empty()
        {
            var request = CreateValidRequest();
            request.Currency = "";

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Currency)
                .WithErrorMessage(ValidationMessages.CurrencyRequired);
        }

        [Theory(DisplayName = "Invalid: Should error when currency is not a 3-letter code")]
        [InlineData("GB")]
        [InlineData("GBPT")]
        [InlineData("  ")]
        [InlineData("E")]
        public void Should_Have_Error_When_Currency_Is_Not_Three_Letters(string currency)
        {
            var request = CreateValidRequest();
            request.Currency = currency;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Currency)
                .WithErrorMessage(ValidationMessages.CurrencyInvalidLength);
        }

        [Theory(DisplayName = "Invalid: Should error when currency is not supported")]
        [InlineData("JPY")]
        [InlineData("AUD")]
        [InlineData("INR")]
        [InlineData("XYZ")]
        public void Should_Have_Error_When_Currency_Is_Not_Supported(string currency)
        {
            // Arrange
            var request = CreateValidRequest();
            request.Currency = currency;

            var allowedCurrencies = Enum.GetNames<Currency>();
            var expectedMessage = ValidationMessages.CurrencyNotSupported(allowedCurrencies);

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Currency)
                .WithErrorMessage(expectedMessage);
        }

        #endregion

        #region Invalid Amount Cases

        [Fact(DisplayName = "Invalid: Should error when amount is zero")]
        public void Should_Have_Error_When_Amount_Is_Zero()
        {
            var request = CreateValidRequest();
            request.Amount = 0;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Amount)
                .WithErrorMessage(ValidationMessages.AmountRequired);
        }

        [Fact(DisplayName = "Invalid: Should error when amount is negative")]
        public void Should_Have_Error_When_Amount_Is_Negative()
        {
            var request = CreateValidRequest();
            request.Amount = -100;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Amount)
                .WithErrorMessage(ValidationMessages.AmountMustBeGreaterThanZero);
        }

        #endregion

        #region Invalid CVV Cases

        [Fact(DisplayName = "Invalid: Should error when CVV is empty")]
        public void Should_Have_Error_When_Cvv_Is_Empty()
        {
            var request = CreateValidRequest();
            request.Cvv = "";

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Cvv)
                .WithErrorMessage(ValidationMessages.CvvRequired);
        }

        [Theory(DisplayName = "Invalid: Should error when CVV length is invalid")]
        [InlineData("1")]
        [InlineData("12")]
        [InlineData("12345")]
        [InlineData("123456")]
        public void Should_Have_Error_When_Cvv_Length_Is_Invalid(string invalidCvv)
        {
            var request = CreateValidRequest();
            request.Cvv = invalidCvv;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Cvv)
                .WithErrorMessage(ValidationMessages.CvvLengthInvalid);
        }

        [Fact(DisplayName = "Invalid: Should error when CVV is more than 4 digits")]
        public void Should_Have_Error_When_Cvv_Is_Too_Long()
        {
            var request = CreateValidRequest();
            request.Cvv = "12345";

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Cvv)
                .WithErrorMessage("CVV must be 3 or 4 digits long.");
        }

        [Theory(DisplayName = "Invalid: Should error when CVV is not numeric")]
        [InlineData("12A")]
        [InlineData("AB3")]
        [InlineData("CVV")]
        [InlineData("1@#")]
        public void Should_Have_Error_When_Cvv_Is_Not_Numeric(string nonNumericCvv)
        {
            var request = CreateValidRequest();
            request.Cvv = nonNumericCvv;

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Cvv)
                .WithErrorMessage(ValidationMessages.CvvNotNumeric);
        }

        #endregion

        #region Helper

        private static PostPaymentRequest CreateValidRequest() => new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1500,
            Cvv = "123"
        };

        #endregion
    }
}
