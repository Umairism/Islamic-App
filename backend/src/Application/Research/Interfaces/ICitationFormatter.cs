using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ICitationFormatter
{
    string Format(ResearchReference reference, string language);
}
