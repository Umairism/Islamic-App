using System.Collections.Generic;

namespace IslamicApp.Application.Research.Interfaces;

public interface ISynonymEngine
{
    List<string> ExpandTokens(List<string> tokens, out Dictionary<string, double> termWeights);
}
