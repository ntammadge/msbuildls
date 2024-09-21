using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class PropertyGroup : IXmlSerializable
{
    public Property[]? Properties { get; set; }

    public XmlSchema? GetSchema()
    {
        throw new System.NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        reader.ReadStartElement();

        var properties = new List<Property>();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            var property = new Property();
            property.ReadXml(reader);
            properties.Add(property);
        }

        if (properties.Any())
        {
            Properties = [.. properties];
        }

        reader.ReadEndElement();
    }

    public void WriteXml(XmlWriter writer)
    {
        throw new System.NotImplementedException();
    }
}