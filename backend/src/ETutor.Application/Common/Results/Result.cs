namespace ETutor.Application.Common.Results;

/// <summary>Outcome of a use case: a value, or one or more expected errors.</summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        Errors = [];
    }

    private Result(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failed result needs at least one error.", nameof(errors));
        }

        Errors = errors;
    }

    public bool IsSuccess => Errors.Count == 0;

    public IReadOnlyList<Error> Errors { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new([error]);

    public static Result<T> Failure(IReadOnlyList<Error> errors) => new(errors);

#pragma warning disable CA2225 // Named alternates exist: Success / Failure.
    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
#pragma warning restore CA2225
}
