using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.RestaurantRegistration.Application.Errors;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Commands;
using DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Interfaces.REST;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Interfaces.REST.Transform;

public static class RestaurantResourceFromEntityAssembler
{
    public static RestaurantResource ToResourceFromEntity(Restaurant restaurant) => new(
        restaurant.Id, restaurant.Name, restaurant.Address, restaurant.Phone, restaurant.Latitude, restaurant.Longitude);
}

public static class RegisteredOwnerResourceFromResultAssembler
{
    public static RegisteredOwnerResource ToResourceFromResult(RestaurantRegistrationResult result) => new(
        result.Owner.UserId, result.Owner.FullName, result.Owner.Email, result.Owner.Role, result.Restaurant.Id,
        result.Restaurant.Name, result.Restaurant.Address, result.Restaurant.Phone, result.Owner.Token);
}

public static class RegisterRestaurantCommandFromResourceAssembler
{
    public static RegisterRestaurantCommand ToCommandFromResource(RegisterRestaurantResource resource) =>
        new(resource.FullName, resource.RestaurantName, resource.Email, resource.Password);
}

public static class UpdateRestaurantCommandFromResourceAssembler
{
    public static UpdateRestaurantCommand ToCommandFromResource(UpdateRestaurantResource resource, int restaurantId) =>
        new(restaurantId, resource.Name, resource.Address ?? string.Empty, resource.Phone ?? string.Empty);
}

/// <summary>Maps RestaurantRegistrationError to HTTP status codes.</summary>
public static class RestaurantRegistrationActionResultAssembler
{
    public static int StatusCodeOf(Enum? error) => error switch
    {
        RestaurantRegistrationError.RestaurantNotFound => StatusCodes.Status404NotFound,
        RestaurantRegistrationError.EmailAlreadyTaken => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, StatusCodeOf, onSuccess);
}
