using Api.Application.Common.Enums;
using System.Diagnostics.CodeAnalysis; // 1. Add this namespace

namespace Api.Application.Common.Models;

public class Result
{
    internal Result(bool succeeded, IEnumerable<string> errors, ErrorTypes? errorType = null)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
        ErrorType = errorType;
    }

    public bool Succeeded { get; init; }

    [MemberNotNullWhen(true, nameof(ErrorType))]
    public bool IsFailure => !Succeeded;

    public ErrorTypes? ErrorType { get; init; }
    public string[] Errors { get; init; }

    public static Result Success()
    {
        return new Result(true, Array.Empty<string>());
    }

    public static Result Failure(IEnumerable<string> errors, ErrorTypes? errorType = ErrorTypes.BadRequest)
    {
        return new Result(false, errors, errorType);
    }

    public static Result BadRequest(IEnumerable<string> errors) => new(false, errors, ErrorTypes.BadRequest);
    public static Result Validation(IEnumerable<string> errors) => new(false, errors, ErrorTypes.Validation);
    public static Result NotFound(IEnumerable<string> errors) => new(false, errors, ErrorTypes.NotFound);
    public static Result Conflict(IEnumerable<string> errors) => new(false, errors, ErrorTypes.Conflict);
    public static Result Unauthorized(IEnumerable<string> errors) => new(false, errors, ErrorTypes.Unauthorized);
}

public class Result<T> : Result
{
    public T? Value { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    public new bool Succeeded => base.Succeeded;

    [MemberNotNullWhen(false, nameof(Value))]
    public new bool IsFailure => base.IsFailure;

    internal Result(T? value, bool succeeded, IEnumerable<string> errors, ErrorTypes? errorType = null)
        : base(succeeded, errors, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value, true, Array.Empty<string>());
    }

    public new static Result<T> Failure(IEnumerable<string> errors, ErrorTypes? errorType = ErrorTypes.BadRequest)
    {
        return new Result<T>(default, false, errors, errorType);
    }

    public new static Result<T> BadRequest(IEnumerable<string> errors) =>
        new(default, false, errors, ErrorTypes.BadRequest);

    public new static Result<T> Validation(IEnumerable<string> errors) =>
        new(default, false, errors, ErrorTypes.Validation);

    public new static Result<T> NotFound(IEnumerable<string> errors) =>
        new(default, false, errors, ErrorTypes.NotFound);

    public new static Result<T> Conflict(IEnumerable<string> errors) =>
        new(default, false, errors, ErrorTypes.Conflict);

    public new static Result<T> Unauthorized(IEnumerable<string> errors) =>
        new(default, false, errors, ErrorTypes.Unauthorized);
}
