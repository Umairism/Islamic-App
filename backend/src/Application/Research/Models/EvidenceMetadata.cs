namespace IslamicApp.Application.Research.Models;

public record EvidenceMetadata(
    string Dataset,
    string Edition,
    string Translator,
    string Language,
    string Version,
    string Checksum
);
