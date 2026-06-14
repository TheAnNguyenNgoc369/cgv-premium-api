namespace CinemaBooking.API.Services;

public interface ITokenRevocationService
{
    void Revoke(string token, DateTime expiresAtUtc);

    bool IsRevoked(string token);
}
