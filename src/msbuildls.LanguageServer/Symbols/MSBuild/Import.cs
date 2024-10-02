using System.Xml;
using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class Import
{
    [XmlAttribute]
    public string Project { get; set; } = string.Empty;
}