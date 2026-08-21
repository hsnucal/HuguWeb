using System.Diagnostics.CodeAnalysis;

namespace HuGuWeb.Workforce.Application;

public readonly struct WorkforceResult<T>
{
    private WorkforceResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private WorkforceResult(WorkforceError error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public T? Value { get; }
    public WorkforceError? Error { get; }

    public static WorkforceResult<T> Success(T value) => new(value);

    public static WorkforceResult<T> Failure(WorkforceError error) => new(error);

    public static implicit operator WorkforceResult<T>(T value) => Success(value);

    public static implicit operator WorkforceResult<T>(WorkforceError error) => Failure(error);
}

public readonly struct WorkforceResult
{
    private WorkforceResult(WorkforceError? error)
    {
        IsSuccess = error is null;
        Error = error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public WorkforceError? Error { get; }

    public static WorkforceResult Success() => new(null);

    public static WorkforceResult Failure(WorkforceError error) => new(error);

    public static implicit operator WorkforceResult(WorkforceError error) => Failure(error);
}
