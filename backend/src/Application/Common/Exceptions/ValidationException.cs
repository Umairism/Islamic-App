using System;
using System.Collections.Generic;

namespace IslamicApp.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();

    public ValidationException() : base("One or more validation failures have occurred.")
    {
    }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string field, string message) : this()
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { message } }
        };
    }

    public ValidationException(IDictionary<string, string[]> errors) : this()
    {
        Errors = errors;
    }
}
