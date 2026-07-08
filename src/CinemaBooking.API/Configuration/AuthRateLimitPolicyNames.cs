namespace CinemaBooking.API.Configuration;

public static class AuthRateLimitPolicyNames
{
    public const string Login = "auth-login";
    public const string Register = "auth-register";
    public const string EmailAction = "auth-email-action";
    public const string Verify = "auth-verify";
}
