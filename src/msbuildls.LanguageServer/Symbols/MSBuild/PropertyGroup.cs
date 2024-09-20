using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class PropertyGroup
{
    [XmlAnyElement] // Must be an any element because otherwise the (de)serializer thinks the property name is the object type
    public Property[]? Properties { get; set; }
}