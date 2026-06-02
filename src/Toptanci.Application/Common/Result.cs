namespace Toptanci.Application.Common;

/// <summary>İşlem sonucunu (başarı/hata) temsil eden tip.</summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Başarılı sonuçta hata olamaz.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Başarısız sonuçta hata zorunludur.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>Değer taşıyan işlem sonucu.</summary>
public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Başarısız sonucun değerine erişilemez.");

    public static implicit operator Result<T>(T value) => Success(value);
}
