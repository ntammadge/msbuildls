using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

public interface IElementWithRange
{
    Range Range { get; set; }
    const int ClientOffset = -1;
}