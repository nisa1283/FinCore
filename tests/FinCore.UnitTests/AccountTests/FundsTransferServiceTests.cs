using FinCore.Account.Application.Services;
using FinCore.Account.Domain.Entities;
using FinCore.BuildingBlocks.Contracts;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.UnitTests.Support;
using FluentAssertions;

namespace FinCore.UnitTests.AccountTests;

public class FundsTransferServiceTests
{
    // Builds two accounts in an in-memory database and the service under test
    private sealed class Scenario
    {
        public TestAccountDbContext Db { get; } = TestAccountDbContext.Create();
        public FundsTransferService Service { get; }
        public BankAccount Source { get; }
        public BankAccount Target { get; }

        public Scenario(
            decimal sourceBalance = 1_000m,
            decimal targetBalance = 500m,
            AccountStatus sourceStatus = AccountStatus.Active,
            AccountStatus targetStatus = AccountStatus.Active,
            string targetCurrency = "TRY")
        {
            Source = new BankAccount
            {
                UserId = Guid.NewGuid(),
                AccountNumber = "TR" + new string('1', 24),
                Name = "Source",
                Balance = sourceBalance,
                Currency = "TRY",
                Status = sourceStatus
            };

            Target = new BankAccount
            {
                UserId = Guid.NewGuid(),
                AccountNumber = "TR" + new string('2', 24),
                Name = "Target",
                Balance = targetBalance,
                Currency = targetCurrency,
                Status = targetStatus
            };

            Db.Accounts.AddRange(Source, Target);
            Db.SaveChanges();

            Service = new FundsTransferService(Db);
        }

        public InternalTransferRequest Request(decimal amount) =>
            new(Source.UserId, Source.Id, Target.AccountNumber, amount, Guid.NewGuid());
    }

    [Fact]
    public async Task Transfer_MovesMoney_WhenRequestIsValid()
    {
        var scenario = new Scenario();

        var response = await scenario.Service.TransferAsync(scenario.Request(250m));

        scenario.Source.Balance.Should().Be(750m);
        scenario.Target.Balance.Should().Be(750m);
        response.SourceBalanceAfter.Should().Be(750m);
        response.ReceiverUserId.Should().Be(scenario.Target.UserId);
    }

    [Fact]
    public async Task Transfer_Succeeds_WhenAmountEqualsTheWholeBalance()
    {
        var scenario = new Scenario(sourceBalance: 300m);

        await scenario.Service.TransferAsync(scenario.Request(300m));

        scenario.Source.Balance.Should().Be(0m);
        scenario.Target.Balance.Should().Be(800m);
    }

    [Fact]
    public async Task Transfer_Throws_WhenBalanceIsInsufficient()
    {
        var scenario = new Scenario(sourceBalance: 100m);

        var act = () => scenario.Service.TransferAsync(scenario.Request(250m));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Insufficient balance*");
        scenario.Source.Balance.Should().Be(100m);
        scenario.Target.Balance.Should().Be(500m);
    }

    [Fact]
    public async Task Transfer_Throws_WhenSourceAccountIsFrozen()
    {
        var scenario = new Scenario(sourceStatus: AccountStatus.Frozen);

        var act = () => scenario.Service.TransferAsync(scenario.Request(50m));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Source account is frozen*");
        scenario.Source.Balance.Should().Be(1_000m);
    }

    [Fact]
    public async Task Transfer_Throws_WhenTargetAccountIsFrozen()
    {
        var scenario = new Scenario(targetStatus: AccountStatus.Frozen);

        var act = () => scenario.Service.TransferAsync(scenario.Request(50m));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Target account is frozen*");
        scenario.Target.Balance.Should().Be(500m);
    }

    [Fact]
    public async Task Transfer_Throws_WhenUserDoesNotOwnTheSourceAccount()
    {
        var scenario = new Scenario();
        var request = new InternalTransferRequest(Guid.NewGuid(), scenario.Source.Id, scenario.Target.AccountNumber, 50m, Guid.NewGuid());

        var act = () => scenario.Service.TransferAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>();
        scenario.Source.Balance.Should().Be(1_000m);
    }

    [Fact]
    public async Task Transfer_Throws_WhenSourceAndTargetAreTheSameAccount()
    {
        var scenario = new Scenario();
        var request = new InternalTransferRequest(scenario.Source.UserId, scenario.Source.Id, scenario.Source.AccountNumber, 50m, Guid.NewGuid());

        var act = () => scenario.Service.TransferAsync(request);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*must be different*");
    }

    [Fact]
    public async Task Transfer_Throws_WhenCurrenciesDiffer()
    {
        var scenario = new Scenario(targetCurrency: "USD");

        var act = () => scenario.Service.TransferAsync(scenario.Request(50m));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*different currencies*");
    }

    [Fact]
    public async Task Transfer_Throws_WhenTargetAccountDoesNotExist()
    {
        var scenario = new Scenario();
        var request = new InternalTransferRequest(scenario.Source.UserId, scenario.Source.Id, "TR" + new string('9', 24), 50m, Guid.NewGuid());

        var act = () => scenario.Service.TransferAsync(request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Transfer_Throws_WhenAmountIsNotPositive(int amount)
    {
        var scenario = new Scenario();

        var act = () => scenario.Service.TransferAsync(scenario.Request(amount));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*greater than zero*");
    }
}