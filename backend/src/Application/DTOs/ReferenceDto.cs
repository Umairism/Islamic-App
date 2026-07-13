namespace IslamicApp.Application.DTOs;

public class ReferenceDto
{
    public string Type { get; set; } = "Quran";
    public string Reference { get; set; } // e.g. "2:255"
    public int GlobalIndex { get; set; }
    public string Language { get; set; } = "ar";
}
