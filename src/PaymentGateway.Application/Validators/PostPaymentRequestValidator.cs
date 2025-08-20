using FluentValidation;
using PaymentGateway.Application.Constants;
using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Validators
{
    public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
    {
        public PostPaymentRequestValidator()
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty().WithMessage(ValidationMessages.CardNumberRequired)
                .Length(14, 19).WithMessage(ValidationMessages.CardNumberLength)
                .Matches(@"^\d+$").WithMessage(ValidationMessages.CardNumberNumeric);

            RuleFor(x => x.ExpiryMonth)
                .NotEmpty().WithMessage(ValidationMessages.ExpiryMonthRequired)
                .InclusiveBetween(1, 12).WithMessage(ValidationMessages.ExpiryMonthOutOfRange);

            RuleFor(x => x.ExpiryYear)
                .NotEmpty().WithMessage(ValidationMessages.ExpiryYearRequired)
                .GreaterThanOrEqualTo(DateTime.UtcNow.Year).WithMessage(ValidationMessages.ExpiryYearTooEarly)
                .Must((request, year) =>
                {
                    try
                    {
                        var expiryDate = new DateTime(year, request.ExpiryMonth, 1).AddMonths(1).AddDays(-1);
                        return expiryDate > DateTime.UtcNow;
                    }
                    catch
                    {
                        return false;
                    }
                }).WithMessage(ValidationMessages.ExpiryDateInPast);

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage(ValidationMessages.CurrencyRequired)
                .Must(currency => currency != null && currency.Trim().Length == 3)
                .WithMessage(ValidationMessages.CurrencyInvalidLength)
                .Must(currency => AllowedCurrencies.Contains(currency.Trim()))
                .WithMessage(ValidationMessages.CurrencyNotSupported(AllowedCurrencies));

            RuleFor(x => x.Amount)
                .NotEmpty().WithMessage(ValidationMessages.AmountRequired)
                .GreaterThan(0).WithMessage(ValidationMessages.AmountMustBeGreaterThanZero);

            RuleFor(x => x.Cvv)
                .NotEmpty().WithMessage(ValidationMessages.CvvRequired)
                .Length(3, 4).WithMessage(ValidationMessages.CvvLengthInvalid)
                .Matches(@"^\d+$").WithMessage(ValidationMessages.CvvNotNumeric);
        }

        private static readonly HashSet<string> AllowedCurrencies = new(
            Enum.GetNames<Currency>(),
            StringComparer.OrdinalIgnoreCase
        );
    }
}
