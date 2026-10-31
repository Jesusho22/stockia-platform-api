namespace DataBit.StockIA.Platform.RestaurantRegistration.Application.Errors;

/// <summary>Errors of the Restaurant Registration bounded context (their names are the problem titles).</summary>
public enum RestaurantRegistrationError
{
    RestaurantNotFound,
    EmailAlreadyTaken,
    DatabaseError
}
