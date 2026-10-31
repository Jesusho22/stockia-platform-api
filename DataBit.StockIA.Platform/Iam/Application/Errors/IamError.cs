namespace DataBit.StockIA.Platform.Iam.Application.Errors;

/// <summary>Errors of the Identity &amp; Access Management bounded context (their names are the problem titles).</summary>
public enum IamError
{
    InvalidCredentials,
    AccountLocked,
    InvalidResetCode,
    EmailAlreadyTaken,
    UserNotFound,
    CurrentPasswordInvalid,
    SamePassword,
    LastAdministrator,
    CannotRemoveYourself,
    DatabaseError
}
