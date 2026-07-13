using System.Collections.Generic;

namespace IslamicApp.Application.Research.Interfaces;

public interface ITokenizer
{
    List<string> Tokenize(string text);
    List<string> RemoveStopwords(List<string> tokens, string language);
}
