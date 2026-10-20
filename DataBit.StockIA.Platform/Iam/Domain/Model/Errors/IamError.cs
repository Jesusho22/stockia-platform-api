namespace DataBit.StockIA.Platform.Iam.Domain.Model.Errors;

public enum IamError
{
    InvalidCredentials,
    EmailAlreadyTaken,
    UserNotFound,
    RestaurantNotFound,
    CurrentPasswordInvalid,
    SamePassword,
    LastAdministrator,
    CannotRemoveYourself,
    DatabaseError
}
