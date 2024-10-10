using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using msbuildls.LanguageServer.Symbols;
using msbuildls.LanguageServer.Symbols.MSBuild;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests.Symbols;

public class SymbolResolverTests
{
    [Fact]
    public void CanResolveProjectPropertySymbolAtLocation()
    {
        var propertyName = "TestProperty";
        var project = new Project()
        {
            PropertyGroups = [
                new PropertyGroup()
                {
                    Properties = [
                        new Property()
                        {
                            Name = "IncorrectProperty",
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(2, 10, 2, 40)
                        },
                        new Property()
                        {
                            Name = propertyName,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(3, 10, 3, 40)
                        }
                    ]
                }
            ]
        };
        var position = new Position(3, 20); // Needs to be in the second property's range

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(project);

        var symbolResolver = new SymbolResolver(NullLogger<ISymbolResolver>.Instance, mockSymbolProvider.Object);

        var resolvedSymbol = symbolResolver.ResolveSymbolAtLocation(@"C:\SomeFile.targets", position);

        Assert.NotNull(resolvedSymbol);
        Assert.IsType<Property>(resolvedSymbol);
        Assert.Equal(propertyName, (resolvedSymbol as Property)!.Name);
    }

    [Fact]
    public void NoSymbolResolvedAtLocationIfNoSymbolsForFile()
    {
        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(null as Project);

        var symbolResolver = new SymbolResolver(NullLogger<ISymbolResolver>.Instance, mockSymbolProvider.Object);

        var resolvedSymbol = symbolResolver.ResolveSymbolAtLocation(@"C:\SomeFile.targets", new Position());

        mockSymbolProvider.Verify(provider => provider.GetFileSymbols(It.IsAny<string>()), Times.Once);
        Assert.Null(resolvedSymbol);
    }

    [Fact]
    public void CanResolveProjectPropertyDefinitionAtDifferentPositionInSameFile()
    {
        var propertyDefinition = new Property()
        {
            Name = "TestProperty",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(4, 10, 4, 30)
        };
        var propertyAtLocation = new Property()
        {
            Name = propertyDefinition.Name,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(5, 10, 5, 50) // Must be after the definition's range
        };
        var project = new Project()
        {
            PropertyGroups = [
                new PropertyGroup()
                {
                    Properties = [
                        new Property()
                        {
                            Name = "OtherProperty",
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(3, 10, 3, 30)
                        },
                        propertyDefinition,
                        propertyAtLocation
                    ]
                }
            ]
        };
        var testFilePath = @"C:\Does\Not\Exist.targets";

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(project);

        var symbolResolver = new SymbolResolver(NullLogger<ISymbolResolver>.Instance, mockSymbolProvider.Object);

        var symbolDefinitionLocation = symbolResolver.ResolveDefinitionForSymbol(propertyAtLocation, testFilePath);

        Assert.NotNull(symbolDefinitionLocation);
        Assert.Equal(testFilePath, symbolDefinitionLocation.Uri.ToUri().LocalPath);
        Assert.Equal(propertyDefinition.Range.Start.Line, symbolDefinitionLocation.Range.Start.Line);
        Assert.Equal(propertyDefinition.Range.Start.Character, symbolDefinitionLocation.Range.Start.Character);
        Assert.Equal(propertyDefinition.Range.End.Line, symbolDefinitionLocation.Range.End.Line);
        Assert.Equal(propertyDefinition.Range.End.Character, symbolDefinitionLocation.Range.End.Character);
    }

    [Fact]
    public void CanResolveProjectPropertyDefinitionAtSamePositionInSameFile()
    {
        var propertyDefinition = new Property()
        {
            Name = "TestProperty",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(4, 10, 4, 30)
        };
        var project = new Project()
        {
            PropertyGroups = [
                new PropertyGroup()
                {
                    Properties = [
                        new Property()
                        {
                            Name = "OtherProperty",
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(3, 10, 3, 30)
                        },
                        propertyDefinition,
                        new Property()
                        {
                            Name = "AnotherProperty",
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(5, 10, 5, 50)
                        }
                    ]
                }
            ]
        };
        var testFilePath = @"C:\Does\Not\Exist.targets";

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(project);

        var symbolResolver = new SymbolResolver(NullLogger<ISymbolResolver>.Instance, mockSymbolProvider.Object);

        var symbolDefinitionLocation = symbolResolver.ResolveDefinitionForSymbol(propertyDefinition, testFilePath);

        Assert.NotNull(symbolDefinitionLocation);
        Assert.Equal(testFilePath, symbolDefinitionLocation.Uri.ToUri().LocalPath);
        Assert.Equal(propertyDefinition.Range.Start.Line, symbolDefinitionLocation.Range.Start.Line);
        Assert.Equal(propertyDefinition.Range.Start.Character, symbolDefinitionLocation.Range.Start.Character);
        Assert.Equal(propertyDefinition.Range.End.Line, symbolDefinitionLocation.Range.End.Line);
        Assert.Equal(propertyDefinition.Range.End.Character, symbolDefinitionLocation.Range.End.Character);
    }

    [Fact]
    public void CanResolveTargetPropertySymbolAtLocation()
    {
        var targetProperty = new Property()
        {
            Name = "TargetProperty",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(4, 11, 4, 50)
        };
        var fileSymbols = new Project()
        {
            Targets = [
                new Target()
                {
                    PropertyGroups = [
                        new PropertyGroup()
                        {
                            Properties = [ targetProperty ]
                        }
                    ]
                }
            ]
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(fileSymbols);

        var symbolResolver = new SymbolResolver(NullLogger<ISymbolResolver>.Instance, mockSymbolProvider.Object);

        var identifiedSymbol = symbolResolver.ResolveSymbolAtLocation(@"C:\SomeFile.targets", new Position(4, 15)); // Position must be in range of the target property

        Assert.NotNull(identifiedSymbol);
        Assert.Equal(targetProperty, identifiedSymbol);
    }

    [Fact]
    public void CanResolveTargetPropertyDefinitionToTargetProperty()
    {
        var identifiedSymbol = new Property()
        {
            Name = "IdentifiedProperty",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(12, 12, 12, 50)
        };
        var symbolDefinition = new Property()
        {
            Name = identifiedSymbol.Name,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(7, 12, 7, 50)
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(new Project()
            {
                Targets = [
                    new Target()
                    {
                        PropertyGroups = [
                            new PropertyGroup()
                            {
                                Properties = [ symbolDefinition ]
                            }
                        ]
                    },
                    new Target()
                    {
                        PropertyGroups = [
                            new PropertyGroup()
                            {
                                Properties = [ identifiedSymbol ]
                            }
                        ]
                    }
                ]
            });

        var symbolResolver = new SymbolResolver(NullLogger<ISymbolResolver>.Instance, mockSymbolProvider.Object);
        var expectedUri = DocumentUri.File(@"C:\TargetScope.targets");

        var symbolDefinitionLocation = symbolResolver.ResolveDefinitionForSymbol(identifiedSymbol, expectedUri.ToUri().LocalPath);
        Assert.NotNull(symbolDefinitionLocation);
        Assert.Equal(expectedUri.ToUri().LocalPath, symbolDefinitionLocation.Uri.ToUri().LocalPath);
        Assert.Equal(symbolDefinition.Range, symbolDefinitionLocation.Range);
    }

    [Fact]
    public void CanResolvePropertyDefinitionFromImport()
    {
        var initialFilePath = @"C:\initial.targets";
        var importedFilePath = @"C:\imported.targets";
        var identifiedProperty = new Property()
        {
            Name = "TestProperty",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(5, 10, 5, 50)
        };
        var importedProperty = new Property()
        {
            Name = identifiedProperty.Name,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(2, 10, 2, 40)
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.Is<string>((file) => file == initialFilePath)))
            .Returns(new Project()
            {
                Imports = [
                    new Import()
                    {
                        Project = Path.GetFileName(importedFilePath),
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(3, 10, 3, 50) // This range must be earlier in the file than the property to trigger looking in the import for a definition
                    }
                ],
                PropertyGroups = [
                    new PropertyGroup()
                    {
                        Properties = [ identifiedProperty ]
                    }
                ]
            });
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.Is<string>((file) => file == importedFilePath)))
            .Returns(new Project()
            {
                PropertyGroups = [
                    new PropertyGroup()
                    {
                        Properties = [ importedProperty ]
                    }
                ]
            });

        var symbolResolver = new SymbolResolver(NullLogger<ISymbolResolver>.Instance, mockSymbolProvider.Object);

        var symbolDefinitionLocation = symbolResolver.ResolveDefinitionForSymbol(identifiedProperty, initialFilePath);

        mockSymbolProvider.Verify(provider => provider.GetFileSymbols(It.Is<string>((filePath) => filePath == initialFilePath)), Times.Once);
        mockSymbolProvider.Verify(provider => provider.GetFileSymbols(It.Is<string>((filePath) => filePath == importedFilePath)), Times.Once);
        Assert.NotNull(symbolDefinitionLocation);
        Assert.Equal(importedFilePath, symbolDefinitionLocation.Uri.ToUri().LocalPath);
        Assert.Equal(importedProperty.Range, symbolDefinitionLocation.Range);
    }
}