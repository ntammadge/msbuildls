using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class Property : IdentifiableElement, IXmlSerializable
{
    public string Value { get; set; } = string.Empty;

    public XmlSchema? GetSchema()
    {
        throw new System.NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        Name = reader.LocalName;

        var startLineInfo = (IXmlLineInfo)reader;
        var startLine = startLineInfo.LineNumber;
        var startCharacter = startLineInfo.LinePosition;

        reader.ReadStartElement();

        if (reader.HasValue) // Only true when a value exists between the start and end tag, and we've read to the value location
        {
            Value = reader.ReadString();
        }

        var endLineInfo = (IXmlLineInfo)reader;
        var endLine = endLineInfo.LineNumber;
        var endCharacter = endLineInfo.LinePosition;

        Range = new Range(startLine + ClientOffset, startCharacter + ClientOffset, endLine + ClientOffset, endCharacter + Name.Length + ClientOffset);

        reader.ReadEndElement();
    }

    public void WriteXml(XmlWriter writer)
    {
        throw new System.NotImplementedException();
    }
}