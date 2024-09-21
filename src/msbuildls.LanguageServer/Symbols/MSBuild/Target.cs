using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class Target
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlElement(nameof(PropertyGroup))]
    public PropertyGroup[]? PropertyGroups { get; set; }
}