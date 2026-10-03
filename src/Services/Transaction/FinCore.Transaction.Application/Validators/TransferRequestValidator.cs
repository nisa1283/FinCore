using FinCore.Transaction.Application.DTOs;
using FinCore.Transaction.Domain.Entities;
using FluentValidation;

namespace FinCore.Transaction.Application.Validators;

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.SourceAccountId).NotEmpty();

        RuleFor(x => x.TargetAccountNumber).NotEmpty().MaximumLength(34);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000m);

        RuleFor(x => x.Amount)
            .Must(a => decimal.Round(a, 2) == a)
            .WithMessage("Amount can have at most 2 decimal places.");

        RuleFor(x => x.Description).MaximumLength(200);

        RuleFor(x => x.Category)
            .Must(c => string.IsNullOrWhiteSpace(c) || TransactionCategories.All.Contains(c))
            .WithMessage("Invalid category.");
    }
}