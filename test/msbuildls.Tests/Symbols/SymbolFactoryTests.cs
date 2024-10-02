using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using msbuildls.LanguageServer.Symbols;
using msbuildls.LanguageServer.Symbols.MSBuild;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests.Symbols;


public class SymbolFactoryTests
{
    private int _symbolOffset = -1;

    private string GetFullyQualifiedTestDataFilePath(string fileName)
    {
        var relativeDirectory = @"..\..\..\..\testData\SymbolFactoryTests";
        return Path.GetFullPath(Path.Combine(relativeDirectory, fileName));
    }

    [Fact]
    public void CanMakePropertySymbol()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);
        var property = new Property()
        {
            Name = "TestProperty",
            Value = "testValue",
            StartLine = 1,
            StartChar = 2,
            EndLine = 1,
            EndChar = 30,
        };
        var project = new Project()
        {
            PropertyGroups = [
                new PropertyGroup()
                {
                    Properties = [ property ]
                }
            ]
        };

        var projectSymbols = symbolFactory.SymbolsForFile(project);
        Assert.NotNull(projectSymbols);
        var propertySymbol = Assert.Single(projectSymbols);
        Assert.NotNull(propertySymbol);
        Assert.NotNull(propertySymbol.DocumentSymbol);
        Assert.Null(propertySymbol.SymbolInformation);
        Assert.Equal(property.Name, propertySymbol.DocumentSymbol.Name);
        Assert.Equal(property.StartLine + _symbolOffset, propertySymbol.DocumentSymbol.Range.Start.Line);
        Assert.Equal(property.StartChar + _symbolOffset, propertySymbol.DocumentSymbol.Range.Start.Character);
        Assert.Equal(property.EndLine + _symbolOffset, propertySymbol.DocumentSymbol.Range.End.Line);
        Assert.Equal(property.EndChar + _symbolOffset, propertySymbol.DocumentSymbol.Range.End.Character);
        Assert.Equal(property.StartLine + _symbolOffset, propertySymbol.DocumentSymbol.SelectionRange.Start.Line);
        Assert.Equal(property.StartChar + _symbolOffset, propertySymbol.DocumentSymbol.SelectionRange.Start.Character);
        Assert.Equal(property.StartLine + _symbolOffset, propertySymbol.DocumentSymbol.SelectionRange.End.Line);
        Assert.Equal(property.StartChar + property.Name.Length + _symbolOffset, propertySymbol.DocumentSymbol.SelectionRange.End.Character);
        Assert.Null(propertySymbol.DocumentSymbol.Children);
    }

    [Fact]
    public void PropertySymbolCreationDoesNotUpdateInternalLineData()
    {
        var startLine = 1;
        var startChar = 1;
        var endLine = 1;
        var endChar = 30;
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);
        var property = new Property()
        {
            Name = "TestProperty",
            Value = "testValue",
            StartLine = startLine,
            StartChar = startChar,
            EndLine = endLine,
            EndChar = endChar,
        };
        var project = new Project()
        {
            PropertyGroups = [
                new PropertyGroup()
                {
                    Properties = [ property ]
                }
            ]
        };

        symbolFactory.SymbolsForFile(project);

        Assert.Equal(startLine, property.StartLine);
        Assert.Equal(startChar, property.StartChar);
        Assert.Equal(endLine, property.EndLine);
        Assert.Equal(endChar, property.EndChar);
    }

    [Fact]
    public void CanParseSimpleFile()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);
        var testFilePath = GetFullyQualifiedTestDataFilePath("project.props");

        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(testFilePath),
            Text = File.ReadAllText(testFilePath)
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);
        Assert.NotNull(fileSymbols);
        Assert.Null(fileSymbols.PropertyGroups);
    }

    [Fact]
    public void CanParseEmptyFile()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);
        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(@"C:\Empty.targets"),
            Text = ""
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);
        Assert.Null(fileSymbols);
    }

    [Fact]
    public void CanParsePropertiesOnProjectNode()
    {
        var expectedProperty = new Property()
        {
            Name = "TestProperty",
            Value = "testValue",
            StartLine = 3,
            StartChar = 10,
            EndLine = 3,
            EndChar = 46
        };

        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);
        var testFilePath = GetFullyQualifiedTestDataFilePath("property.props");

        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(testFilePath),
            Text = File.ReadAllText(testFilePath)
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);
        Assert.NotNull(fileSymbols);
        Assert.NotNull(fileSymbols.PropertyGroups);
        var propGroup = Assert.Single(fileSymbols.PropertyGroups);
        Assert.NotNull(propGroup.Properties);
        var parsedProperty = Assert.Single(propGroup.Properties);
        Assert.Equal(expectedProperty.Name, parsedProperty.Name);
        Assert.Equal(expectedProperty.Value, parsedProperty.Value);
        Assert.Equal(expectedProperty.StartLine, parsedProperty.StartLine);
        Assert.Equal(expectedProperty.StartChar, parsedProperty.StartChar);
        Assert.Equal(expectedProperty.EndLine, parsedProperty.EndLine);
        Assert.Equal(expectedProperty.EndChar, parsedProperty.EndChar);
    }

    [Fact]
    public void CanParseEmptyPropertyGroup()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);
        var testFilePath = GetFullyQualifiedTestDataFilePath("propGroup.props");

        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(testFilePath),
            Text = File.ReadAllText(testFilePath)
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);
        Assert.NotNull(fileSymbols);
        Assert.NotNull(fileSymbols.PropertyGroups);
        var propGroup = Assert.Single(fileSymbols.PropertyGroups);
        Assert.Null(propGroup.Properties);
    }

    [Fact]
    public void CanDeserializeTarget()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);
        var testFilePath = GetFullyQualifiedTestDataFilePath("target.targets");

        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(testFilePath),
            Text = File.ReadAllText(testFilePath)
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);
        Assert.NotNull(fileSymbols);
        Assert.NotNull(fileSymbols.Targets);
        var target = Assert.Single(fileSymbols.Targets);
        Assert.Equal("TestTarget", target.Name);
        Assert.Equal(2, target.StartPosition.Line);
        Assert.Equal(6, target.StartPosition.Character);
        Assert.Equal(3, target.EndPosition.Line);
        Assert.Equal(13, target.EndPosition.Character);
        Assert.Null(target.PropertyGroups);
    }

    [Fact]
    public void CanDeserializePropertyOnTarget()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);
        var testFilePath = GetFullyQualifiedTestDataFilePath("targetWithProperty.targets");

        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(testFilePath),
            Text = File.ReadAllText(testFilePath)
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);
        Assert.NotNull(fileSymbols);
        Assert.NotNull(fileSymbols.Targets);
        var target = Assert.Single(fileSymbols.Targets);
        Assert.NotNull(target.PropertyGroups);
        var targetPropGroup = Assert.Single(target.PropertyGroups);
        Assert.NotNull(targetPropGroup.Properties);
        var targetProperty = Assert.Single(targetPropGroup.Properties);
        Assert.Equal("TargetProperty", targetProperty.Name);
        /// Property deserialization fully tested in <see cref="CanParsePropertiesOnProjectNode"/>
    }

    [Fact]
    public void CanMakeTargetSymbol()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);

        var testTarget = new Target()
        {
            Name = "TestTarget",
            StartPosition = new Position(2, 6),
            EndPosition = new Position(3, 13)
        };
        var fileSymbols = new Project()
        {
            Targets = [ testTarget ]
        };

        var symbolContainer = symbolFactory.SymbolsForFile(fileSymbols);

        Assert.NotNull(symbolContainer);
        var targetSymbol = Assert.Single(symbolContainer);
        Assert.NotNull(targetSymbol.DocumentSymbol);
        Assert.Null(targetSymbol.SymbolInformation);
        Assert.Equal(testTarget.Name, targetSymbol.DocumentSymbol.Name);
        Assert.Equal((SymbolKind)MsBuildSymbolKind.Target, targetSymbol.DocumentSymbol.Kind);
        Assert.Equal(testTarget.StartPosition.Line + _symbolOffset, targetSymbol.DocumentSymbol.Range.Start.Line);
        Assert.Equal(testTarget.StartPosition.Character + _symbolOffset, targetSymbol.DocumentSymbol.Range.Start.Character);
        Assert.Equal(testTarget.EndPosition.Line + _symbolOffset, targetSymbol.DocumentSymbol.Range.End.Line);
        Assert.Equal(testTarget.EndPosition.Character + _symbolOffset, targetSymbol.DocumentSymbol.Range.End.Character);
        Assert.Equal(testTarget.StartPosition.Line + _symbolOffset, targetSymbol.DocumentSymbol.SelectionRange.Start.Line);
        Assert.Equal(testTarget.StartPosition.Character + _symbolOffset, targetSymbol.DocumentSymbol.SelectionRange.Start.Character);
        Assert.Equal(testTarget.StartPosition.Line + _symbolOffset, targetSymbol.DocumentSymbol.SelectionRange.End.Line);
        Assert.Equal(testTarget.StartPosition.Character + _symbolOffset, targetSymbol.DocumentSymbol.SelectionRange.End.Character);
        Assert.Null(targetSymbol.DocumentSymbol.Children);
    }

    [Fact]
    public void CanMakePropertySymbolOnTarget()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);

        var testProperty = new Property()
        {
            Name = "TargetProperty",
            StartLine = 4,
            StartChar = 14,
            EndLine = 4,
            EndChar = 54
        };
        var testTarget = new Target()
        {
            Name = "TestTarget",
            StartPosition = new Position(2, 6),
            EndPosition = new Position(3, 13),
            PropertyGroups = [
                new PropertyGroup()
                {
                    Properties = [ testProperty ]
                }
            ]
        };
        var fileSymbols = new Project()
        {
            Targets = [ testTarget ]
        };

        var symbolContainer = symbolFactory.SymbolsForFile(fileSymbols);

        Assert.NotNull(symbolContainer);
        var symbol = Assert.Single(symbolContainer);
        Assert.NotNull(symbol.DocumentSymbol);
        Assert.Equal(testTarget.Name, symbol.DocumentSymbol.Name);
        Assert.NotNull(symbol.DocumentSymbol.Children);
        var targetPropertySymbol = Assert.Single(symbol.DocumentSymbol.Children);
        Assert.NotNull(targetPropertySymbol);
        Assert.Equal((SymbolKind)MsBuildSymbolKind.Property, targetPropertySymbol.Kind);
        Assert.Equal(testProperty.Name, targetPropertySymbol.Name);
        /// Property symbol creation tested in full at <see cref="CanMakePropertySymbol"/>
    }

    [Fact]
    public void CanDeserializeProjectImport()
    {
        var expectedImport = new Import()
        {
            Project = "Imported.targets"
        };

        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);

        var testFilePath = GetFullyQualifiedTestDataFilePath("import.targets");

        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(testFilePath),
            Text = File.ReadAllText(testFilePath)
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);

        Assert.NotNull(fileSymbols);
        Assert.NotNull(fileSymbols.Imports);
        var deserializedImport = Assert.Single(fileSymbols.Imports);
        Assert.NotNull(deserializedImport);
        Assert.Equal(expectedImport.Project, deserializedImport.Project);
    }

    [Fact]
    public void CanDeserializeImportGroup()
    {
        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);

        var testFilePath = GetFullyQualifiedTestDataFilePath("importGroup.targets");

        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(testFilePath),
            Text = File.ReadAllText(testFilePath)
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);

        Assert.NotNull(fileSymbols);
        Assert.NotNull(fileSymbols.ImportGroups);
        var deserializedImportGroup = Assert.Single(fileSymbols.ImportGroups);
        Assert.NotNull(deserializedImportGroup);
    }

    [Fact]
    public void CanDeserializeImportGroupImport()
    {
        var import = new Import()
        {
            Project = "Imported.targets"
        };

        var symbolFactory = new SymbolFactory(NullLogger<ISymbolFactory>.Instance);

        var testFilePath = GetFullyQualifiedTestDataFilePath("importGroupImport.targets");

        var textDocumentItem = new TextDocumentItem()
        {
            Uri = new Uri(testFilePath),
            Text = File.ReadAllText(testFilePath)
        };

        var fileSymbols = symbolFactory.ParseDocument(textDocumentItem);

        Assert.NotNull(fileSymbols);
        Assert.NotNull(fileSymbols.ImportGroups);
        var deserializedImportGroup = Assert.Single(fileSymbols.ImportGroups);
        Assert.NotNull(deserializedImportGroup);
        Assert.NotNull(deserializedImportGroup.Imports);
        var deserializedImport = Assert.Single(deserializedImportGroup.Imports);
        Assert.Equal(import.Project, deserializedImport.Project);
    }
}