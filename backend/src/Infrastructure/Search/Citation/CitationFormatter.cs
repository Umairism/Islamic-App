using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Citation;

public class CitationFormatter : ICitationFormatter
{
    private readonly IEnumerable<ICitationStrategy> _strategies;

    public CitationFormatter(IEnumerable<ICitationStrategy> strategies)
    {
        _strategies = strategies;
    }

    public string Format(ResearchReference reference, string language)
    {
        if (reference == null) return string.Empty;

        var strategy = _strategies.FirstOrDefault(s => s.Source == reference.Source);
        if (strategy != null)
        {
            return strategy.Format(reference, language);
        }

        // Fallback default citation
        return reference.ToDisplayString();
    }
}
