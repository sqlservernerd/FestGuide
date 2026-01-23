namespace FestConnect.Domain.Exceptions;

/// <summary>
/// Exception thrown when authentication fails.
/// </summary>
public class AuthenticationException : DomainException
{
    public AuthenticationException(string message) : base(message)
    {
    }

    public static AuthenticationException InvalidCredentials() =>
        new("Invalid email or password.");

    public static AuthenticationException AccountLocked(DateTime lockoutEndUtc) =>
        new($"Account is locked until {lockoutEndUtc:u}. Please try again later.");

    public static AuthenticationException EmailNotVerified() =>
        new("Email address has not been verified. Please check your email for the verification link.");

    public static AuthenticationException InvalidRefreshToken() =>
            new("Invalid or expired refresh token.");

        public static AuthenticationException TokenExpired() =>
            new("Access token has expired.");

        public static AuthenticationException InvalidVerificationToken() =>
            new("Invalid or expired email verification token.");

        public static AuthenticationException InvalidPasswordResetToken() =>
            new("Invalid or expired password reset token.");
    }
