namespace DataBit.StockIA.Platform.Shared.Application.Model;

/// <summary>
///     Outcome of a command handler: a value, or an error code plus a
///     localized message. <see cref="Details" /> carries extra data for the
///     client (e.g. the supplies that block a sale).
/// </summary>
/// <typeparam name="T">Type of the value on success.</typeparam>
public class Result<T>
{
    protected Result(bool isSuccess, T? value, string message, Enum? error, object? details)
    {
        IsSuccess = isSuccess;
        Value = value;
        Message = message;
        Error = error;
        Details = details;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string Message { get; }
    public Enum? Error { get; }
    public object? Details { get; }

    public static Result<T> Success(T value) => new(true, value, string.Empty, null, null);

    public static Result<T> Failure(Enum error, string message, object? details = null) =>
        new(false, default, message, error, details);
}

/// <summary>
///     Result without a value.
/// </summary>
public class Result : Result<object>
{
    private Result(bool isSuccess, string message, Enum? error, object? details)
        : base(isSuccess, null, message, error, details)
    {
    }

    public static Result Success() => new(true, string.Empty, null, null);

    public new static Result Failure(Enum error, string message, object? details = null) =>
        new(false, message, error, details);
}
