using FinCore.Auth.Application.Abstractions;
using FinCore.Auth.Application.DTOs;
using FinCore.Auth.Application.Services;
using FinCore.Auth.Application.Validators;
using FinCore.Auth.Domain.Entities;
using FinCore.BuildingBlocks.Exceptions;
using FinCore.UnitTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinCore.UnitTests.AuthTests;

public class AuthServiceTests
{
    private sealed class Sut
    {
        public TestAuthDbContext Db { get; } = TestAuthDbContext.Create();
        public Mock<IPasswordHasher> Hasher { get; } = new();
        public Mock<IJwtTokenService> Tokens { get; } = new();
        public AuthService Service { get; }

        public Sut()
        {
            Hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");

            Tokens.Setup(t => t.CreateAccessToken(It.IsAny<User>()))
                .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));

            Tokens.Setup(t => t.CreateRefreshToken(It.IsAny<Guid>()))
                .Returns((Guid userId) => new RefreshToken
                {
                    UserId = userId,
                    Token = Guid.NewGuid().ToString("N"),
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                });

            Service = new AuthService(
                Db,
                Hasher.Object,
                Tokens.Object,
                new RegisterRequestValidator(),
                new LoginRequestValidator(),
                new ChangePasswordRequestValidator(),
                new UpdateProfileRequestValidator());
        }

        public void PasswordIsCorrect(bool correct) =>
            Hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(correct);

        public async Task<User> AddUserAsync(string email = "user@test.com")
        {
            var user = new User { Email = email, FullName = "Test User", PasswordHash = "hashed" };
            Db.Users.Add(user);
            await Db.SaveChangesAsync();
            return user;
        }
    }

    [Fact]
    public async Task Login_LocksTheAccount_AfterFiveFailedAttempts()
    {
        var sut = new Sut();
        var user = await sut.AddUserAsync();
        sut.PasswordIsCorrect(false);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var act = () => sut.Service.LoginAsync(new LoginRequest("user@test.com", "WrongPass1"));
            await act.Should().ThrowAsync<UnauthorizedAppException>();
        }

        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_IsRejected_WhileTheAccountIsLocked_EvenWithTheCorrectPassword()
    {
        var sut = new Sut();
        var user = await sut.AddUserAsync();
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
        await sut.Db.SaveChangesAsync();
        sut.PasswordIsCorrect(true);

        var act = () => sut.Service.LoginAsync(new LoginRequest("user@test.com", "Correct123"));

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*locked*");
    }

    [Fact]
    public async Task Login_ResetsTheFailedCounter_WhenThePasswordIsCorrect()
    {
        var sut = new Sut();
        var user = await sut.AddUserAsync();
        user.FailedLoginCount = 3;
        await sut.Db.SaveChangesAsync();
        sut.PasswordIsCorrect(true);

        var response = await sut.Service.LoginAsync(new LoginRequest("user@test.com", "Correct123"));

        response.AccessToken.Should().Be("access-token");
        user.FailedLoginCount.Should().Be(0);
        (await sut.Db.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Register_Throws_WhenTheEmailIsAlreadyRegistered()
    {
        var sut = new Sut();
        await sut.AddUserAsync("taken@test.com");

        var act = () => sut.Service.RegisterAsync(new RegisterRequest("TAKEN@test.com", "Test1234", "Someone"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Register_StoresTheHashedPassword_AndNormalizesTheEmail()
    {
        var sut = new Sut();

        var profile = await sut.Service.RegisterAsync(new RegisterRequest("New@Test.com", "Test1234", "New User"));

        profile.Email.Should().Be("new@test.com");
        var saved = await sut.Db.Users.SingleAsync();
        saved.PasswordHash.Should().Be("hashed");
        saved.PasswordHash.Should().NotContain("Test1234");
    }
}