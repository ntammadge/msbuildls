using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class Import : IElementWithRange, IXmlSerializable
{
    [XmlAttribute]
    public string Project { get; set; } = string.Empty;
    public Range Range { get; set; } = new Range(0, 0, 0, 0);

    public XmlSchema? GetSchema()
    {
        throw new System.NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        var startLine = ((IXmlLineInfo)reader).LineNumber;
        var startCharacter = ((IXmlLineInfo)reader).LinePosition;

        var endLine = startLine;
        var endCharacter = 0;

        // Read all attributes in order to calculate an accurate end position for the import element
        while (reader.MoveToNextAttribute())
        {
            if (reader.LocalName == nameof(Project))
            {
                Project = reader.Value;
            }

            endLine = ((IXmlLineInfo)reader).LineNumber;
            endCharacter = ((IXmlLineInfo)reader).LinePosition + reader.ReadOuterXml().Length;
        }

        Range = new Range(
            startLine + IElementWithRange.ClientOffset,
            startCharacter + IElementWithRange.ClientOffset,
            endLine + IElementWithRange.ClientOffset,
            endCharacter + IElementWithRange.ClientOffset
        );
        reader.MoveToElement();
        reader.Read();
    }

    public void WriteXml(XmlWriter writer)
    {
        throw new System.NotImplementedException();
    }
}