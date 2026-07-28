namespace SiPacul.Shared.Results;

public class Result
{
    protected Result(
        bool isSuccess,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (isSuccess &&
            error != Error.None)
        {
            throw new InvalidOperationException(
                "Successful result cannot contain an error.");
        }

        if (!isSuccess &&
            error == Error.None)
        {
            throw new InvalidOperationException(
                "Failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success()
    {
        return new Result(
            true,
            Error.None);
    }

    public static Result Failure(Error error)
    {
        return new Result(
            false,
            error);
    }
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value)
        : base(
            true,
            Error.None)
    {
        _value = value;
    }

    private Result(Error error)
        : base(
            false,
            error)
    {
    }

    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException(
                    "A failed result does not contain a value.");
            }

            return _value!;
        }
    }

    public static Result<TValue> Success(
        TValue value)
    {
        return new Result<TValue>(value);
    }

    public static new Result<TValue> Failure(
        Error error)
    {
        return new Result<TValue>(error);
    }
}
