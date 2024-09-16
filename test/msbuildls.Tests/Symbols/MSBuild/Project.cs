using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class Project
{
    [XmlElement(nameof(PropertyGroup))]
    public PropertyGroup[] PropertyGroups { get; set; } = [];
}