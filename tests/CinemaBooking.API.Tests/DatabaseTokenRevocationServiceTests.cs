using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using CinemaBooking.API.Services;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Tests;

public sealed class DatabaseTokenRevocationServiceTests
{
    [Fact]
    public async Task RevokedTokenIsRejected()
    {
        var state = new SharedUserState();
        var service = CreateService(state);
        var token = CreateToken(state.UserId, state.TokenVersion);

        await service.RevokeAsync(token, DateTime.UtcNow.AddMinutes(5));

        Assert.True(await service.IsRevokedAsync(token));
    }

    [Fact]
    public async Task RevocationSurvivesServiceRecreation()
    {
        var state = new SharedUserState();
        var token = CreateToken(state.UserId, state.TokenVersion);

        await CreateService(state).RevokeAsync(token, DateTime.UtcNow.AddMinutes(5));
        var recreatedService = CreateService(state);

        Assert.True(await recreatedService.IsRevokedAsync(token));
    }

    [Fact]
    public async Task MultipleServiceInstancesShareRevocationState()
    {
        var state = new SharedUserState();
        var firstInstance = CreateService(state);
        var secondInstance = CreateService(state);
        var token = CreateToken(state.UserId, state.TokenVersion);

        await firstInstance.RevokeAsync(token, DateTime.UtcNow.AddMinutes(5));

        Assert.True(await secondInstance.IsRevokedAsync(token));
    }

    [Fact]
    public async Task ExpiredTokenIsNotPersistedAsARevocation()
    {
        var state = new SharedUserState();
        var service = CreateService(state);
        var token = CreateToken(state.UserId, state.TokenVersion);

        await service.RevokeAsync(token, DateTime.UtcNow.AddMinutes(-1));

        Assert.Equal(0, state.TokenVersion);
        Assert.False(await service.IsRevokedAsync(token));
    }

    private static DatabaseTokenRevocationService CreateService(SharedUserState state)
    {
        var repository = DispatchProxy.Create<IUserRepository, UserRepositoryProxy>();
        ((UserRepositoryProxy)(object)repository).State = state;
        return new DatabaseTokenRevocationService(repository);
    }

    private static string CreateToken(int userId, int tokenVersion)
    {
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtTokenService.TokenVersionClaim, tokenVersion.ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(5));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class SharedUserState
    {
        public object SyncRoot { get; } = new();
        public int UserId { get; } = 42;
        public int TokenVersion { get; set; }
    }

    private class UserRepositoryProxy : DispatchProxy
    {
        public SharedUserState State { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                nameof(IUserRepository.GetByIdAsync) => GetByIdAsync((int)args![0]!),
                nameof(IUserRepository.TryIncrementTokenVersionAsync) =>
                    TryIncrementTokenVersionAsync((int)args![0]!, (int)args[1]!),
                _ => throw new NotSupportedException(targetMethod?.Name)
            };
        }

        private Task<User?> GetByIdAsync(int userId)
        {
            lock (State.SyncRoot)
            {
                User? user = userId == State.UserId
                    ? new User
                    {
                        UserID = State.UserId,
                        Status = "active",
                        TokenVersion = State.TokenVersion
                    }
                    : null;

                return Task.FromResult(user);
            }
        }

        private Task<bool> TryIncrementTokenVersionAsync(int userId, int expectedTokenVersion)
        {
            lock (State.SyncRoot)
            {
                if (userId != State.UserId || State.TokenVersion != expectedTokenVersion)
                {
                    return Task.FromResult(false);
                }

                State.TokenVersion++;
                return Task.FromResult(true);
            }
        }
    }
}
