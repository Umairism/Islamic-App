using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Exceptions;

namespace IslamicApp.WebApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid?.ToString() : string.Empty;
        
        var statusCode = StatusCodes.Status500InternalServerError;
        var title = "Internal Server Error";
        var type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
        var detail = exception.Message;
        object errors = null;

        if (exception is NotFoundException notFoundEx)
        {
            statusCode = StatusCodes.Status404NotFound;
            title = "Not Found";
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
            _logger.LogWarning("Resource not found: {Message} (CorrelationId: {CorrelationId})", notFoundEx.Message, correlationId);
        }
        else if (exception is ValidationException validationEx)
        {
            statusCode = StatusCodes.Status400BadRequest;
            title = "Validation Failed";
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            detail = "One or more validation errors occurred.";
            
            var list = new List<object>();
            if (validationEx.Errors != null)
            {
                foreach (var kvp in validationEx.Errors)
                {
                    foreach (var msg in kvp.Value)
                    {
                        list.Add(new { field = kvp.Key, message = msg });
                    }
                }
            }
            errors = list;
            
            _logger.LogWarning("Validation failed: {Message} (CorrelationId: {CorrelationId})", validationEx.Message, correlationId);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message} (CorrelationId: {CorrelationId})", exception.Message, correlationId);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new
        {
            type,
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            errors = errors ?? new object[0],
            correlationId
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }
}
