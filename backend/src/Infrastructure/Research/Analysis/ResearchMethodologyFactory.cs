using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class ResearchMethodologyFactory : IResearchMethodologyFactory
{
    private readonly Dictionary<ResearchMethodologyType, IResearchMethodology> _methodologies;

    public ResearchMethodologyFactory(IEnumerable<IResearchMethodology> methodologies)
    {
        _methodologies = methodologies.ToDictionary(m => m.Type);
    }

    public IResearchMethodology CreateMethodology(ResearchMethodologyType type)
    {
        if (_methodologies.TryGetValue(type, out var strategy))
        {
            return strategy;
        }

        throw new ArgumentException($"Methodology type {type} is not registered in the system.");
    }
}
