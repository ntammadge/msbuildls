using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Symbols.MSBuild;

public class Target : IXmlSerializable
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlElement(nameof(PropertyGroup))]
    public PropertyGroup[]? PropertyGroups { get; set; }
    public Position StartPosition { get; set; } = new Position(1, 1);
    public Position EndPosition { get; set; } = new Position(1,1);

    public XmlSchema? GetSchema()
    {
        throw new System.NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        var startLineInfo = (IXmlLineInfo)reader;
        StartPosition = new Position(startLineInfo.LineNumber, startLineInfo.LinePosition);

        Name = reader.GetAttribute(nameof(Name)) ?? string.Empty;
        reader.ReadStartElement();

        var propGroups = new List<PropertyGroup>();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.LocalName == nameof(PropertyGroup))
            {
                var propGroup = new PropertyGroup();
                propGroup.ReadXml(reader);
                propGroups.Add(propGroup);
            }
        }

        if (propGroups.Any())
        {
            PropertyGroups = [.. propGroups];
        }

        var endLineInfo = (IXmlLineInfo)reader;
        EndPosition = new Position(endLineInfo.LineNumber, endLineInfo.LinePosition + nameof(Target).Length);
        reader.ReadEndElement();
    }

    public void WriteXml(XmlWriter writer)
    {
        throw new System.NotImplementedException();
    }
}