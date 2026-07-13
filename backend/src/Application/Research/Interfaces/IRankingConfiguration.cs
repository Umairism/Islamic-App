namespace IslamicApp.Application.Research.Interfaces;

public interface IRankingConfiguration
{
    double Reference { get; }
    double Alias { get; }
    double Arabic { get; }
    double Translation { get; }
    double SurahName { get; }
    double Synonym { get; }
    double Partial { get; }
    string Checksum { get; }
}
