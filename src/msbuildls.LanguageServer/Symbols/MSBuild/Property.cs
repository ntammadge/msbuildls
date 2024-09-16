using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class Property : IXmlSerializable
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public XmlSchema? GetSchema()
    {
        throw new System.NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        Name = reader.LocalName;
        reader.ReadStartElement();

        // TODO: read attributes in here somewhere

        if (reader.HasValue) // Only true when a value exists between the start and end tag, and we've read to the value location
        {
            Value = reader.ReadString();
        }
        reader.ReadEndElement();
    }

    public void WriteXml(XmlWriter writer)
    {
        throw new System.NotImplementedException();
    }
}