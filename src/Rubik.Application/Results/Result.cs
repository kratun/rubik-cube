namespace Rubik.Application.Results;

/// <summary>The outcome of a use case: a value or a typed <see cref="ApplicationError"/>, never both (AD-6).</summary>
public sealed class Result<T>
{
    private const string NoValueMessage = "A failed result has no value.";
    private const string NoErrorMessage = "A successful result has no error.";

    private readonly T? value;
    private readonly ApplicationError? error;

    private Result(T? value, ApplicationError? error)
    {
        this.value = value;
        this.error = error;
    }

    public bool IsSuccess => error is null;

    /// <summary>The value of a successful result. Throws when the result is a failure.</summary>
    public T Value => IsSuccess ? value! : throw new InvalidOperationException(NoValueMessage);

    /// <summary>The error of a failed result. Throws when the result is a success.</summary>
    public ApplicationError Error => error ?? throw new InvalidOperationException(NoErrorMessage);

    public static Result<T> Success(T value) => new(value, error: null);

    public static Result<T> Failure(ApplicationError error) =>
        new(value: default, error ?? throw new ArgumentNullException(nameof(error)));
}
