using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Domain.Services;

namespace DataBit.StockIA.Platform.Tests.Iam;

public class AccountLockAndPasswordResetTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.FromHours(-5));

    private static User Employee() => new("Jesus Miranda", "Empleado@DataBiteCorp.com ", "hash", 1, UserRole.Employee);

    [Fact]
    public void Five_wrong_passwords_lock_the_account_for_fifteen_minutes()
    {
        var user = Employee();
        for (var attempt = 1; attempt < AccountLockService.MaxFailedAttempts; attempt++)
        {
            Assert.False(AccountLockService.RegisterFailedAttempt(user, Now));
            Assert.Equal(AccountLockService.MaxFailedAttempts - attempt, AccountLockService.RemainingAttempts(user));
        }

        Assert.True(AccountLockService.RegisterFailedAttempt(user, Now));
        Assert.True(user.IsLockedAt(Now.AddMinutes(14)));
        Assert.False(user.IsLockedAt(Now.AddMinutes(15)));
        Assert.Equal(15, AccountLockService.MinutesLeft(user, Now));
    }

    [Fact]
    public void Unlocking_clears_the_failed_attempts()
    {
        var user = Employee();
        for (var attempt = 0; attempt < AccountLockService.MaxFailedAttempts; attempt++) AccountLockService.RegisterFailedAttempt(user, Now);

        user.Unlock();

        Assert.False(user.IsLockedAt(Now));
        Assert.Equal(AccountLockService.MaxFailedAttempts, AccountLockService.RemainingAttempts(user));
    }

    [Fact]
    public void A_reset_code_has_six_digits_and_expires()
    {
        Assert.Matches("^\\d{6}$", PasswordResetService.NewCode());

        var user = Employee().StartPasswordReset("code-hash", Now.Add(PasswordResetService.CodeLifetime));
        Assert.True(PasswordResetService.CanUseCode(user, Now.AddMinutes(29)));
        Assert.False(PasswordResetService.CanUseCode(user, Now.AddMinutes(31)));
    }

    [Fact]
    public void A_reset_code_stops_working_after_too_many_wrong_tries()
    {
        var user = Employee().StartPasswordReset("code-hash", Now.AddMinutes(30));
        for (var attempt = 0; attempt < PasswordResetService.MaxAttempts; attempt++) user.RegisterFailedPasswordReset();

        Assert.False(PasswordResetService.CanUseCode(user, Now));
    }

    [Fact]
    public void The_email_shown_after_a_reset_request_is_masked()
    {
        Assert.Equal("e******o@databitecorp.com", PasswordResetService.MaskEmail("empleado@databitecorp.com"));
        Assert.Equal("empleado@databitecorp.com", Employee().Email);
    }
}
