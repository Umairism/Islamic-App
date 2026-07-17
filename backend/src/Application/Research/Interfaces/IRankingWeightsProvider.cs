using System.Collections.Generic;

namespace IslamicApp.Application.Research.Interfaces;

public interface IRankingWeightsProvider
{
    double GetWeight(string factor);
    IReadOnlyDictionary<string, double> GetAllWeights();
}
