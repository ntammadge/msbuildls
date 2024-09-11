using System.IO;
using System.Linq;
using System.Xml.Linq;
using msbuildls.LanguageServer.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests;


public class SymbolFactoryTests
{
    [Fact]
    public void CanMakePropertySymbol()
    {
        var startLine = 2;
        var startChar = 9;
        var name = "TestProperty";
        var expectedSymbol = new DocumentSymbol()
        {
            Name = name,
            Kind = (SymbolKind)MsBuildSymbolKind.Property,
            Range = new Range(
                startLine : startLine,
                startCharacter: startChar,
                endLine: startLine,
                endCharacter: startChar + name.Length
            )
        };

        var symbolFactory = new SymbolFactory();

        var xml = XElement.Parse(File.ReadAllText(@"..\..\..\..\testData\SymbolFactoryTests\singleProperty.props"), LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var propertyNode = xml.Descendants("TestProperty").First();

        var propertySymbol = symbolFactory.MakePropertyFromNode(propertyNode);

        Assert.Equal(expectedSymbol.Name, propertySymbol.Name);
        Assert.Equal(expectedSymbol.Range.Start.Line, propertySymbol.Range.Start.Line);
        Assert.Equal(expectedSymbol.Range.Start.Character, propertySymbol.Range.Start.Character);
        Assert.Equal(expectedSymbol.Range.End.Line, propertySymbol.Range.End.Line);
        Assert.Equal(expectedSymbol.Range.End.Character, propertySymbol.Range.End.Character);
        Assert.Equal(expectedSymbol.Kind, propertySymbol.Kind);
    }

    [Fact]
    public void CanMakeProjectSymbol()
    {
        var name = KnownMsBuildNodes.Project;
        var startLine = 0;
        var startChar = 1;
        var expectedSymbol = new DocumentSymbol()
        {
            Name = name,
            Kind = (SymbolKind)MsBuildSymbolKind.Project,
            Range = new Range(
                startLine: startLine,
                startCharacter: startChar,
                endLine: startLine,
                endCharacter: startChar + name.Length)
        };

        var symbolFactory = new SymbolFactory();
        var xml = XElement.Parse(File.ReadAllText(@"..\..\..\..\testData\SymbolFactoryTests\simpleProject.props"), LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        var documentSymbol = symbolFactory.MakeDocumentSymbols(xml);

        Assert.Equal(expectedSymbol.Name, documentSymbol.Name);
        Assert.Equal(expectedSymbol.Kind, documentSymbol.Kind);
        Assert.Equal(expectedSymbol.Range.Start.Line, documentSymbol.Range.Start.Line);
        Assert.Equal(expectedSymbol.Range.Start.Character, documentSymbol.Range.Start.Character);
        Assert.Equal(expectedSymbol.Range.End.Line, documentSymbol.Range.End.Line);
        Assert.Equal(expectedSymbol.Range.End.Character, documentSymbol.Range.End.Character);
    }

    [Fact]
    public void CanNestPropertiesOnProject()
    {
        var symbolFactory = new SymbolFactory();

        var xml = XElement.Parse(File.ReadAllText(@"..\..\..\..\testData\SymbolFactoryTests\singleProperty.props"), LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var projectSymbol = symbolFactory.MakeDocumentSymbols(xml);

        Assert.NotNull(projectSymbol.Children);
        var propertySymbol = Assert.Single(projectSymbol.Children);

        Assert.Equal("TestProperty", propertySymbol.Name);
        Assert.Equal((SymbolKind)MsBuildSymbolKind.Property, propertySymbol.Kind);
    }

    [Fact]
    public void DoNotMakePropertyGroupSymbol()
    {
        var symbolFactory = new SymbolFactory();

        var xml = XElement.Parse(File.ReadAllText(@"..\..\..\..\testData\SymbolFactoryTests\singleProperty.props"), LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var projectSymbol = symbolFactory.MakeDocumentSymbols(xml);

        Assert.NotNull(projectSymbol.Children);
        Assert.DoesNotContain(projectSymbol.Children, child => child.Name == KnownMsBuildNodes.PropertyGroup);
    }
}