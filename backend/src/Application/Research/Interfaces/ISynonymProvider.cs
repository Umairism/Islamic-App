using System.Collections.Generic;

namespace IslamicApp.Application.Research.Interfaces;

public interface ISynonymProvider
{
    IReadOnlyCollection<string> GetSynonyms(string word);
}
