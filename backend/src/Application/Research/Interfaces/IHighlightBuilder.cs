using System.Collections.Generic;

namespace IslamicApp.Application.Research.Interfaces;

public interface IHighlightBuilder
{
    List<string> BuildHighlights(string text, List<string> terms);
}
