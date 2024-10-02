using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class ImportGroup
{
    [XmlElement(nameof(Import))]
    public Import[]? Imports { get; set; }
}