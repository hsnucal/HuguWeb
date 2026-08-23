using System.Diagnostics.CodeAnalysis;

namespace HuGuWeb.TechnicalService.Application;

public readonly struct TechnicalServiceResult<T>
{
    private TechnicalServiceResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private TechnicalServiceResult(TechnicalServiceError error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public T? Value { get; }
    public TechnicalServiceError? Error { get; }

    public static TechnicalServiceResult<T> Success(T value) => new(value);

    public static TechnicalServiceResult<T> Failure(TechnicalServiceError error) => new(error);

    public static implicit operator TechnicalServiceResult<T>(T value) => Success(value);

    public static implicit operator TechnicalServiceResult<T>(TechnicalServiceError error) => Failure(error);
}
