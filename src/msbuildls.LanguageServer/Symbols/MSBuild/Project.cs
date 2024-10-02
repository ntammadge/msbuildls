using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

/// <summary>
/// The root-level element of every MSBuild file, including in files that aren't project files (ex. csproj)
/// </summary>
public class Project
{
    [XmlElement(nameof(PropertyGroup))]
    public PropertyGroup[]? PropertyGroups { get; set; }

    [XmlElement(nameof(Target))]
    public Target[]? Targets { get; set; }
}