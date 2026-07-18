using System;

namespace IslamicApp.Application.Research.Models;

public readonly record struct DocumentId(string Value);
public readonly record struct NodeId(string Value);
public readonly record struct ReferenceId(string Value);
public readonly record struct TopicId(string Value);

public enum ErrorSeverity
{
    Information,
    Warning,
    Error,
    Critical
}

public sealed record Error(
    string Code,
    string Message,
    ErrorSeverity Severity
);

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
}

public enum ConfidenceLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

public readonly record struct ConfidenceScore
{
    public double Value { get; }
    public ConfidenceLevel Level { get; }

    public ConfidenceScore(double value)
    {
        if (value < 0.0 || value > 1.0)
            throw new ArgumentOutOfRangeException(nameof(value), "Confidence value must be between 0.0 and 1.0.");

        Value = value;
        Level = value switch
        {
            < 0.4 => ConfidenceLevel.Low,
            < 0.7 => ConfidenceLevel.Medium,
            < 0.9 => ConfidenceLevel.High,
            _ => ConfidenceLevel.VeryHigh
        };
    }

    public static ConfidenceScore From(double value) => new(value);
}
