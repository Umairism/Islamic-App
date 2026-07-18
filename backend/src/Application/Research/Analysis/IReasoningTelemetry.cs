using System;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IReasoningTelemetry
{
    void TrackUsage(GenerationMetadata metadata);
    void TrackRetry(string provider, int attempt, Exception ex);
    void TrackCircuitBreak(string provider, TimeSpan duration);
    void TrackValidationFailure(ValidationReport report);
}
