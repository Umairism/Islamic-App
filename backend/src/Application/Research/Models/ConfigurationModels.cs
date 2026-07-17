using System;
using System.Collections.Generic;

namespace IslamicApp.Application.Research.Models;

public record ConfigurationManifest(
    string Version,
    string Checksum,
    DateTime LoadedAt,
    IReadOnlyList<string> SourceFiles
);

public record SearchConfiguration(
    IReadOnlyDictionary<string, List<string>> Synonyms,
    IReadOnlyDictionary<string, string> Aliases,
    IReadOnlySet<string> StopWords,
    IReadOnlyDictionary<string, double> RankingWeights,
    IReadOnlyDictionary<int, string> SurahNames,
    ConfigurationManifest Manifest
);
