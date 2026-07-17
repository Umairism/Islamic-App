using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IslamicApp.Application.Semantic.Query;

public record ConceptOntology(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("aliases")] IReadOnlyList<string> Aliases,
    [property: JsonPropertyName("roots")] IReadOnlyList<string> Roots,
    [property: JsonPropertyName("related")] IReadOnlyList<string> Related,
    [property: JsonPropertyName("broader")] IReadOnlyList<string> Broader,
    [property: JsonPropertyName("narrower")] IReadOnlyList<string> Narrower,
    [property: JsonPropertyName("seeAlso")] IReadOnlyList<string> SeeAlso
);
