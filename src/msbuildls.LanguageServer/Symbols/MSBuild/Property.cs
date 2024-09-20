using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class Property : IXmlSerializable
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int StartLine { get; set; } = 1;
    public int StartChar { get; set; } = 1;
    public int EndLine { get; set; } = 1;
    /// <summary>
    /// The line position of the end of the property name in the end tag
    /// </summary>
    public int EndChar { get; set; } = 1;

    public XmlSchema? GetSchema()
    {
        throw new System.NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        Name = reader.LocalName;
        var lineInfo = (IXmlLineInfo)reader;
        StartLine = lineInfo.LineNumber;
        StartChar = lineInfo.LinePosition;
        reader.ReadStartElement();

        // TODO: read attributes in here somewhere

        if (reader.HasValue) // Only true when a value exists between the start and end tag, and we've read to the value location
        {
            Value = reader.ReadString();
        }

        lineInfo = (IXmlLineInfo)reader;
        EndLine = lineInfo.LineNumber;
        EndChar = lineInfo.LinePosition + Name.Length;
        reader.ReadEndElement();
    }

    public void WriteXml(XmlWriter writer)
    {
        throw new System.NotImplementedException();
    }
}