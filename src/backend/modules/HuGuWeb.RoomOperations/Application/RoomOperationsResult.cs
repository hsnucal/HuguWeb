using System.Diagnostics.CodeAnalysis;

namespace HuGuWeb.RoomOperations.Application;

public readonly struct RoomOperationsResult<T>
{
    private RoomOperationsResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private RoomOperationsResult(RoomOperationsError error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public T? Value { get; }
    public RoomOperationsError? Error { get; }

    public static RoomOperationsResult<T> Success(T value) => new(value);

    public static RoomOperationsResult<T> Failure(RoomOperationsError error) => new(error);

    public static implicit operator RoomOperationsResult<T>(T value) => Success(value);

    public static implicit operator RoomOperationsResult<T>(RoomOperationsError error) => Failure(error);
}
