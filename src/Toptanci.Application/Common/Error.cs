namespace Toptanci.Application.Common;

/// <summary>İşlem hatalarını temsil eden tip. Kod + mesaj + tür içerir.</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static Error NotFound(string message, string code = "NotFound")
        => new(code, message, ErrorType.NotFound);

    public static Error Validation(string message, string code = "Validation")
        => new(code, message, ErrorType.Validation);

    public static Error Conflict(string message, string code = "Conflict")
        => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string message, string code = "Unauthorized")
        => new(code, message, ErrorType.Unauthorized);

    public static Error Failure(string message, string code = "Failure")
        => new(code, message, ErrorType.Failure);
}

public enum ErrorType
{
    None = 0,
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5
}
