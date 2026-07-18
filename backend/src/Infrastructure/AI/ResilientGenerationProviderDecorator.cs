using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.AI;

public class ResilientGenerationProviderDecorator : ITextGenerationProvider
{
    private readonly ITextGenerationProvider _innerProvider;
    private readonly IReasoningTelemetry _telemetry;
    private readonly int _maxRetries = 3;

    // Static dictionary tracking circuit state per provider name
    private static readonly ConcurrentDictionary<string, CircuitState> CircuitStates = new();

    public ResilientGenerationProviderDecorator(ITextGenerationProvider innerProvider, IReasoningTelemetry telemetry)
    {
        _innerProvider = innerProvider;
        _telemetry = telemetry;
    }

    public string ProviderName => _innerProvider.ProviderName;
    public bool SupportsJsonMode => _innerProvider.SupportsJsonMode;
    public bool SupportsStreaming => _innerProvider.SupportsStreaming;
    public bool SupportsSeed => _innerProvider.SupportsSeed;
    public bool SupportsVision => _innerProvider.SupportsVision;
    public bool SupportsTools => _innerProvider.SupportsTools;

    public async Task<GenerationResponse> GenerateAsync(
        ResearchPrompt prompt,
        GenerationOptions options,
        CancellationToken cancellationToken)
    {
        var state = CircuitStates.GetOrAdd(ProviderName, _ => new CircuitState());

        if (state.IsOpen())
        {
            throw new InvalidOperationException($"Circuit breaker is OPEN for provider {ProviderName}. Please try again later.");
        }

        int attempt = 0;
        int delayMs = 1000;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                var response = await _innerProvider.GenerateAsync(prompt, options, cancellationToken);
                state.RecordSuccess();
                return response;
            }
            catch (Exception ex) when (IsRetriable(ex))
            {
                state.RecordFailure();
                _telemetry.TrackRetry(ProviderName, attempt, ex);

                if (state.IsOpen())
                {
                    _telemetry.TrackCircuitBreak(ProviderName, state.BreakDuration);
                    throw new InvalidOperationException($"Circuit breaker is now OPEN for provider {ProviderName} due to repeated transient failures: {ex.Message}", ex);
                }

                if (attempt >= _maxRetries)
                {
                    throw;
                }

                await Task.Delay(delayMs, cancellationToken);
                delayMs *= 2; // Exponential backoff
            }
            catch (Exception)
            {
                // Non-retriable exception (e.g. 401, 403, 400, Canceled)
                state.RecordFailure(); // Also count towards failure to circuit break if user keeps invoking bad credentials
                throw;
            }
        }
    }

    private bool IsRetriable(Exception ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        
        // Skip retrying auth, key invalidation or client request errors
        if (msg.Contains("401") || msg.Contains("403") || msg.Contains("unauthorized") || msg.Contains("forbidden") || msg.Contains("400") || msg.Contains("bad request") || msg.Contains("invalid_api_key"))
        {
            return false;
        }

        // Retry on 429, 5xx, timeout, or lost connection
        return msg.Contains("429") || 
               msg.Contains("500") || 
               msg.Contains("502") || 
               msg.Contains("503") || 
               msg.Contains("504") || 
               msg.Contains("timeout") || 
               msg.Contains("connection") || 
               ex is TimeoutException ||
               ex is System.Net.Http.HttpRequestException;
    }

    private class CircuitState
    {
        private int _failures = 0;
        private DateTimeOffset? _openedAt = null;
        public TimeSpan BreakDuration { get; } = TimeSpan.FromSeconds(30);

        public bool IsOpen()
        {
            if (_openedAt == null) return false;

            if (DateTimeOffset.UtcNow - _openedAt.Value > BreakDuration)
            {
                // Reset circuit breaker (Half-open transition)
                _openedAt = null;
                _failures = 0;
                return false;
            }

            return true;
        }

        public void RecordSuccess()
        {
            _failures = 0;
            _openedAt = null;
        }

        public void RecordFailure()
        {
            _failures++;
            if (_failures >= 5)
            {
                _openedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
