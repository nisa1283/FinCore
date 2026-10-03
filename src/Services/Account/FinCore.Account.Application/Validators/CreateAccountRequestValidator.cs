using FinCore.Account.Application.DTOs;
using FluentValidation;

namespace FinCore.Account.Application.Validators;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    private static readonly string[] AllowedCurrencies = { "TRY", "USD", "EUR" };

    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Currency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(c => AllowedCurrencies.Contains(c.Trim().ToUpperInvariant()))
            .WithMessage("Currency must be TRY, USD or EUR.");
    }
}