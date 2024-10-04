using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols;

public abstract class IdentifiableElement
{
    public string Name { get; set; } = string.Empty;
    public Range Range { get; set; } = new Range(-1, -1, -1, -1);
    /// <summary>
    /// The known position offset for VSCode. May need to change to support other clients
    /// </summary>
    protected const int ClientOffset = -1;
}