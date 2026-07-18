namespace IslamicApp.Application.Research.Memory;

public interface IConfidenceCalculator
{
    ConfidenceResult Calculate(CompositeConfidence confidence);
}
