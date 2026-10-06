using FinCore.BuildingBlocks.Contracts;
using FinCore.BuildingBlocks.Events;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.BuildingBlocks.Messaging;
using FinCore.Transaction.Application.Abstractions;
using FinCore.Transaction.Application.DTOs;
using FinCore.Transaction.Application.Services;
using FinCore.Transaction.Application.Validators;
using FinCore.Transaction.Domain.Entities;
using FinCore.UnitTests.Support;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinCore.UnitTests.TransferTests;

public class TransactionServiceTests
{
    private static readonly string TargetNumber = "TR" + new string('2', 24);

    // Wires the service with an in-memory database and mocked external dependencies
    private sealed class Sut
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid ReceiverId { get; } = Guid.NewGuid();
        public TestTransactionDbContext Db { get; } = TestTransactionDbContext.Create();
        public Mock<IAccountClient> AccountClient { get; } = new();
        public Mock<IIdempotencyService> Idempotency { get; } = new();
        public Mock<IEventPublisher> Publisher { get; } = new();
        public TransactionService Service { get; }

        public Sut(bool keyIsFree = true)
        {
            Idempotency
                .Setup(i => i.TryAcquireAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(keyIsFree);

            Service = new TransactionService(
                Db,
                AccountClient.Object,
                Idempotency.Object,
                new TransferRequestValidator(),
                Publisher.Object,
                NullLogger<TransactionService>.Instance);
        }

        public TransferRequest Request(decimal amount = 100m, string? category = "Food") =>
            new(Guid.NewGuid(), TargetNumber, amount, "Test transfer", category);

        public void AccountServiceSucceeds() =>
            AccountClient
                .Setup(c => c.TransferAsync(It.IsAny<InternalTransferRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InternalTransferResponse(
                    Guid.NewGuid(), "TR" + new string('1', 24), UserId,
                    Guid.NewGuid(), TargetNumber, ReceiverId,
                    "TRY", 900m));

        public void AccountServiceFails(AppException exception) =>
            AccountClient
                .Setup(c => c.TransferAsync(It.IsAny<InternalTransferRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

        public void AccountServiceWasNeverCalled() =>
            AccountClient.Verify(
                c => c.TransferAsync(It.IsAny<InternalTransferRequest>(), It.IsAny<CancellationToken>()),
                Times.Never());
    }

    // ---------- successful transfer ----------

    [Fact]
    public async Task Transfer_Completes_AndPublishesAnEvent()
    {
        var sut = new Sut();
        sut.AccountServiceSucceeds();

        var response = await sut.Service.TransferAsync(sut.UserId, "key-1", sut.Request(100m));

        response.Status.Should().Be("Completed");
        response.IsDuplicate.Should().BeFalse();

        var saved = await sut.Db.Transactions.SingleAsync();
        saved.Status.Should().Be(TransactionStatus.Completed);
        saved.ReceiverUserId.Should().Be(sut.ReceiverId);
        saved.Currency.Should().Be("TRY");

        sut.Publisher.Verify(
            p => p.PublishAsync(It.IsAny<TransactionCompletedEvent>(), MessagingConstants.TransactionCompletedRoutingKey),
            Times.Once());
    }

    [Fact]
    public async Task Transfer_StillCompletes_WhenPublishingTheEventFails()
    {
        var sut = new Sut();
        sut.AccountServiceSucceeds();
        sut.Publisher
            .Setup(p => p.PublishAsync(It.IsAny<TransactionCompletedEvent>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ is down"));

        var response = await sut.Service.TransferAsync(sut.UserId, "key-2", sut.Request());

        response.Status.Should().Be("Completed");
    }

    // ---------- rejected by the account service ----------

    [Theory]
    [InlineData("Insufficient balance.")]
    [InlineData("Source account is frozen.")]
    public async Task Transfer_RecordsAFailedTransaction_WhenTheAccountServiceRejectsIt(string reason)
    {
        var sut = new Sut();
        sut.AccountServiceFails(new BusinessRuleException(reason));

        var act = () => sut.Service.TransferAsync(sut.UserId, "key-3", sut.Request());

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage(reason);

        var saved = await sut.Db.Transactions.SingleAsync();
        saved.Status.Should().Be(TransactionStatus.Failed);
        saved.FailureReason.Should().Be(reason);

        sut.Publisher.Verify(
            p => p.PublishAsync(It.IsAny<TransactionCompletedEvent>(), It.IsAny<string>()),
            Times.Never());
    }

    // ---------- duplicate transfer ----------

    [Fact]
    public async Task Transfer_ReturnsTheOriginalResult_WhenTheKeyWasAlreadyUsed()
    {
        var sut = new Sut(keyIsFree: false);

        sut.Db.Transactions.Add(new BankTransaction
        {
            IdempotencyKey = "dup",
            SenderUserId = sut.UserId,
            SourceAccountId = Guid.NewGuid(),
            TargetAccountNumber = TargetNumber,
            Amount = 100m,
            Currency = "TRY",
            Status = TransactionStatus.Completed
        });
        await sut.Db.SaveChangesAsync();

        var response = await sut.Service.TransferAsync(sut.UserId, "dup", sut.Request());

        response.IsDuplicate.Should().BeTrue();
        response.Status.Should().Be("Completed");
        sut.AccountServiceWasNeverCalled();
        (await sut.Db.Transactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Transfer_ThrowsConflict_WhenTheSameRequestIsStillBeingProcessed()
    {
        var sut = new Sut(keyIsFree: false); // key is taken but no transaction was saved yet

        var act = () => sut.Service.TransferAsync(sut.UserId, "in-flight", sut.Request());

        await act.Should().ThrowAsync<ConflictException>();
        sut.AccountServiceWasNeverCalled();
    }

    // ---------- validation ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Transfer_Throws_WhenAmountIsNotPositive(int amount)
    {
        var sut = new Sut();

        var act = () => sut.Service.TransferAsync(sut.UserId, "key-v1", sut.Request(amount));

        await act.Should().ThrowAsync<ValidationException>();
        sut.AccountServiceWasNeverCalled();
    }

    [Fact]
    public async Task Transfer_Throws_WhenAmountHasMoreThanTwoDecimals()
    {
        var sut = new Sut();

        var act = () => sut.Service.TransferAsync(sut.UserId, "key-v2", sut.Request(10.555m));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Transfer_Throws_WhenAmountIsAboveTheLimit()
    {
        var sut = new Sut();

        var act = () => sut.Service.TransferAsync(sut.UserId, "key-v3", sut.Request(1_000_001m));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Transfer_Throws_WhenCategoryIsUnknown()
    {
        var sut = new Sut();

        var act = () => sut.Service.TransferAsync(sut.UserId, "key-v4", sut.Request(category: "Gambling"));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Transfer_Throws_WhenTheIdempotencyKeyIsMissing()
    {
        var sut = new Sut();

        var act = () => sut.Service.TransferAsync(sut.UserId, "  ", sut.Request());

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Idempotency-Key*");
        sut.AccountServiceWasNeverCalled();
    }

    // ---------- risk scoring is stored with the transaction ----------

    [Fact]
    public async Task Transfer_IsFlaggedSuspicious_WhenAHighAmountGoesToANewReceiver()
    {
        var sut = new Sut();
        sut.AccountServiceSucceeds();

        await sut.Service.TransferAsync(sut.UserId, "risk-1", sut.Request(8_000m));

        var saved = await sut.Db.Transactions.SingleAsync();
        saved.RiskScore.Should().Be(60);
        saved.IsSuspicious.Should().BeTrue();
        saved.RiskReasons.Should().Contain("HighAmount").And.Contain("NewReceiver");
    }

    [Fact]
    public async Task Transfer_IsNotSuspicious_WhenTheReceiverIsAlreadyKnown()
    {
        var sut = new Sut();
        sut.AccountServiceSucceeds();

        // An earlier completed transfer to the same account makes the receiver "known"
        sut.Db.Transactions.Add(new BankTransaction
        {
            IdempotencyKey = "old",
            SenderUserId = sut.UserId,
            SourceAccountId = Guid.NewGuid(),
            TargetAccountNumber = TargetNumber,
            Amount = 10m,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });
        await sut.Db.SaveChangesAsync();

        await sut.Service.TransferAsync(sut.UserId, "risk-2", sut.Request(8_000m));

        var saved = await sut.Db.Transactions.SingleAsync(t => t.IdempotencyKey == "risk-2");
        saved.RiskScore.Should().Be(40);
        saved.IsSuspicious.Should().BeFalse();
    }
}