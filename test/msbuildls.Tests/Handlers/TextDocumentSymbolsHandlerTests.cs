using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using msbuildls.LanguageServer.Handlers;
using msbuildls.LanguageServer.Symbols;
using msbuildls.LanguageServer.Symbols.MSBuild;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests.Handlers;

public class TextDocumentSymbolsHandlerTests
{
    [Fact]
    public async Task HandleSymbolDataRequestAsync()
    {
        var request = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };
        var property = new Property()
        {
            Name = "TestProperty",
            Value = "SomeValue",
            StartLine = 3,
            StartChar = 10,
            EndLine = 3,
            EndChar = 40 // Note: not required to be realistic for this test
        };

        var fileSymbols = new Project()
        {
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties = [ property ]
                }
            ]
        };

        var logger = NullLogger<TextDocumentSymbolsHandler>.Instance;

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(fileSymbols)
            .Callback<string>((filePath) => Assert.Equal(request.TextDocument.Uri.Path, filePath));

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.SymbolsForFile(It.IsAny<Project>()))
            .Returns(SymbolInformationOrDocumentSymbolContainer
                .From(new []
                {
                    SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                    {
                        Name = property.Name,
                        Kind = (SymbolKind)MSBuildSymbolKind.Property,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property.StartLine, property.StartChar, property.EndLine, property.EndChar),
                        SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property.StartLine, property.StartChar, property.StartLine, property.StartChar + property.Name.Length)
                    })
                }))
            .Callback<Project>((inputSymbols) => Assert.Equal(fileSymbols, inputSymbols));

        var handler = new TextDocumentSymbolsHandler(logger, mockSymbolFactory.Object, mockSymbolProvider.Object);

        var result = await handler.Handle(request, new CancellationToken());

        mockSymbolProvider.Verify(provider => provider.GetFileSymbols(It.IsAny<string>()), Times.Once);
        mockSymbolFactory.Verify(factory => factory.SymbolsForFile(It.IsAny<Project>()), Times.Once);

        Assert.NotNull(result);
        var symbol = Assert.Single(result);
        Assert.NotNull(symbol.DocumentSymbol);
        Assert.Null(symbol.SymbolInformation);
        Assert.Equal(property.Name, symbol.DocumentSymbol.Name);
        Assert.Equal(property.StartLine, symbol.DocumentSymbol.Range.Start.Line);
        Assert.Equal(property.StartChar, symbol.DocumentSymbol.Range.Start.Character);
        Assert.Equal(property.EndLine, symbol.DocumentSymbol.Range.End.Line);
        Assert.Equal(property.EndChar, symbol.DocumentSymbol.Range.End.Character);
        Assert.Equal(property.StartLine, symbol.DocumentSymbol.SelectionRange.Start.Line);
        Assert.Equal(property.StartChar, symbol.DocumentSymbol.SelectionRange.Start.Character);
        Assert.Equal(property.StartLine, symbol.DocumentSymbol.SelectionRange.End.Line);
        Assert.Equal(property.StartChar + property.Name.Length, symbol.DocumentSymbol.SelectionRange.End.Character);
    }

    [Fact]
    public async Task NullWhenSymbolsNotFoundAsync()
    {
        var docSymbolParams = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\test.targets")
            }
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(default(Project?))
            .Callback<string>(docPath => Assert.Equal(docSymbolParams.TextDocument.Uri.Path, docPath));

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        var logger = NullLogger<TextDocumentSymbolsHandler>.Instance;

        var handler = new TextDocumentSymbolsHandler(logger, mockSymbolFactory.Object, mockSymbolProvider.Object);

        var result = await handler.Handle(docSymbolParams, new CancellationToken());

        mockSymbolProvider.Verify(provider => provider.GetFileSymbols(It.IsAny<string>()), Times.Once);
        mockSymbolFactory.Verify(factory => factory.SymbolsForFile(It.IsAny<Project>()), Times.Never);

        Assert.Null(result);
    }

    [Fact]
    public async Task FilterMultipleOccurrencesOfPropertiesAsync()
    {
        var logger = NullLogger<TextDocumentSymbolsHandler>.Instance;

        var property1 = new Property()
        {
            Name = "FirstProperty",
            Value = "FirstValue",
            StartLine = 3,
            StartChar = 10,
            EndLine = 3,
            EndChar = 40
        };
        var property2 = new Property()
        {
            Name = "FirstProperty", // Matching name to the first property required to represent the same symbol
            Value = "SecondValue",
            StartLine = 4,
            StartChar = 10,
            EndLine = 4,
            EndChar = 40
        };
        var fileSymbols = new Project()
        {
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties =
                    [
                        property1,
                        property2
                    ]
                }
            ]
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(fileSymbols);

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.SymbolsForFile(It.IsAny<Project>()))
            .Returns(SymbolInformationOrDocumentSymbolContainer
                .From(new[]
                {
                    SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                    {
                        Name = property1.Name,
                        Kind = (SymbolKind)MSBuildSymbolKind.Property,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property1.StartLine, property1.StartChar, property1.EndLine, property1.EndChar),
                        SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property1.StartLine, property1.StartChar, property1.StartLine, property1.StartChar + property1.Name.Length)
                    }),
                    SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                    {
                        Name = property2.Name,
                        Kind = (SymbolKind)MSBuildSymbolKind.Property,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property2.StartLine, property2.StartChar, property2.EndLine, property2.EndChar),
                        SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property2.StartLine, property2.StartChar, property2.StartLine, property2.StartChar + property2.Name.Length)
                    })
                }));

        var handler = new TextDocumentSymbolsHandler(logger, mockSymbolFactory.Object, mockSymbolProvider.Object);
        var request = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        Assert.NotNull(result);
        var symbol = Assert.Single(result);
        Assert.NotNull(symbol.DocumentSymbol);
        Assert.Null(symbol.SymbolInformation);
        // Confirming the name and start line are appropriate. The rest of the symbol location/range unnecessary
        Assert.Equal(property1.Name, symbol.DocumentSymbol.Name);
        Assert.Equal(property1.StartLine, symbol.DocumentSymbol.Range.Start.Line);
    }

    [Fact]
    public async Task FilterProjectPropertiesFromTargetSymbolsAsync()
    {
        var logger = NullLogger<TextDocumentSymbolsHandler>.Instance;

        var projectProperty = new Property()
        {
            Name = "TestProperty",
            StartLine = 3,
            StartChar = 10,
            EndLine = 3,
            EndChar = 40
        };
        var targetProperty = new Property()
        {
            Name = projectProperty.Name, // Must have the same name as the project-level property for a valid test
            StartLine = 7,
            StartChar = 14,
            EndLine = 7,
            EndChar = 44
        };
        var target = new Target()
        {
            Name = "TestTarget",
            StartPosition = new Position(5, 5),
            EndPosition = new Position(9, 5),
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties = [ targetProperty ]
                }
            ]
        };
        var fileSymbols = new Project()
        {
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties =
                    [
                        projectProperty
                    ]
                }
            ],
            Targets = [ target ]
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(fileSymbols);

        var symbolFactory = new Mock<ISymbolFactory>();
        symbolFactory
            .Setup(factory => factory.SymbolsForFile(It.IsAny<Project>()))
            .Returns(SymbolInformationOrDocumentSymbolContainer.From(new[]
            {
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = projectProperty.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Property,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(projectProperty.StartLine, projectProperty.StartChar, projectProperty.EndLine, projectProperty.EndChar),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(projectProperty.StartLine, projectProperty.StartChar, projectProperty.StartLine, projectProperty.StartChar + projectProperty.Name.Length)
                }),
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = target.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.StartPosition, target.EndPosition),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.StartPosition, target.StartPosition),
                    Children = new[]
                    {
                        new DocumentSymbol()
                        {
                            Name = targetProperty.Name,
                            Kind = (SymbolKind)MSBuildSymbolKind.Property,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty.StartLine, targetProperty.StartChar, targetProperty.EndLine, targetProperty.EndChar),
                            SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty.StartLine, targetProperty.StartChar, targetProperty.StartLine, targetProperty.StartChar + targetProperty.Name.Length)
                        }
                    }
                })
            }));

        var handler = new TextDocumentSymbolsHandler(logger, symbolFactory.Object, mockSymbolProvider.Object);
        var request = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        Assert.NotNull(result);
        var foundProperty = Assert.Single(result, symbol => symbol.DocumentSymbol?.Name == projectProperty.Name);
        Assert.NotNull(foundProperty.DocumentSymbol);
        Assert.Equal(projectProperty.StartLine, foundProperty.DocumentSymbol.Range.Start.Line); // Ensure we find the symbol at the correct line, indicating we filtered out the reference in the target
    }

    [Fact]
    public async Task DoNotFilterExclusiveTargetPropertyAsync()
    {
        var logger = NullLogger<TextDocumentSymbolsHandler>.Instance;

        var projectProperty = new Property()
        {
            Name = "TestProperty",
            StartLine = 3,
            StartChar = 10,
            EndLine = 3,
            EndChar = 40
        };
        var targetProperty = new Property()
        {
            Name = "TargetProperty", // Must have a different name as the project-level property for a valid test
            StartLine = 7,
            StartChar = 14,
            EndLine = 7,
            EndChar = 44
        };
        var target = new Target()
        {
            Name = "TestTarget",
            StartPosition = new Position(5, 5),
            EndPosition = new Position(9, 5),
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties = [ targetProperty ]
                }
            ]
        };
        var fileSymbols = new Project()
        {
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties =[ projectProperty ]
                }
            ],
            Targets = [ target ]
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(fileSymbols);

        var symbolFactory = new Mock<ISymbolFactory>();
        symbolFactory
            .Setup(factory => factory.SymbolsForFile(It.IsAny<Project>()))
            .Returns(SymbolInformationOrDocumentSymbolContainer.From(new[]
            {
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = projectProperty.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Property,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(projectProperty.StartLine, projectProperty.StartChar, projectProperty.EndLine, projectProperty.EndChar),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(projectProperty.StartLine, projectProperty.StartChar, projectProperty.StartLine, projectProperty.StartChar + projectProperty.Name.Length)
                }),
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = target.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.StartPosition, target.EndPosition),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.StartPosition, target.StartPosition),
                    Children = new[]
                    {
                        new DocumentSymbol()
                        {
                            Name = targetProperty.Name,
                            Kind = (SymbolKind)MSBuildSymbolKind.Property,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty.StartLine, targetProperty.StartChar, targetProperty.EndLine, targetProperty.EndChar),
                            SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty.StartLine, targetProperty.StartChar, targetProperty.StartLine, targetProperty.StartChar + targetProperty.Name.Length)
                        }
                    }
                })
            }));

        var handler = new TextDocumentSymbolsHandler(logger, symbolFactory.Object, mockSymbolProvider.Object);
        var request = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        Assert.NotNull(result);
        var foundProperty = Assert.Single(result, symbol => symbol.DocumentSymbol?.Name == targetProperty.Name);
    }

    [Fact]
    public async Task FilterTargetPropertyReferenceOnSameTargetAsync()
    {
        var logger = NullLogger<TextDocumentSymbolsHandler>.Instance;

        var targetProperty = new Property()
        {
            Name = "TestProperty",
            StartLine = 7,
            StartChar = 14,
            EndLine = 7,
            EndChar = 44
        };
        var targetProperty2 = new Property()
        {
            Name = targetProperty.Name,
            StartLine = targetProperty.StartLine + 1,
            StartChar = targetProperty.StartChar,
            EndLine = targetProperty.StartLine + 1,
            EndChar = targetProperty.EndChar
        };
        var target = new Target()
        {
            Name = "TestTarget",
            StartPosition = new Position(5, 5),
            EndPosition = new Position(9, 5),
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties = [ targetProperty, targetProperty2 ]
                }
            ]
        };
        var fileSymbols = new Project()
        {
            Targets = [ target ]
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(fileSymbols);

        var symbolFactory = new Mock<ISymbolFactory>();
        symbolFactory
            .Setup(factory => factory.SymbolsForFile(It.IsAny<Project>()))
            .Returns(SymbolInformationOrDocumentSymbolContainer.From(new[]
            {
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = target.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.StartPosition, target.EndPosition),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.StartPosition, target.StartPosition),
                    Children = new[]
                    {
                        new DocumentSymbol()
                        {
                            Name = targetProperty.Name,
                            Kind = (SymbolKind)MSBuildSymbolKind.Property,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty.StartLine, targetProperty.StartChar, targetProperty.EndLine, targetProperty.EndChar),
                            SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty.StartLine, targetProperty.StartChar, targetProperty.StartLine, targetProperty.StartChar + targetProperty.Name.Length)
                        },
                        new DocumentSymbol()
                        {
                            Name = targetProperty2.Name,
                            Kind = (SymbolKind)MSBuildSymbolKind.Property,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty2.StartLine, targetProperty2.StartChar, targetProperty2.EndLine, targetProperty2.EndChar),
                            SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty2.StartLine, targetProperty2.StartChar, targetProperty2.StartLine, targetProperty2.StartChar + targetProperty2.Name.Length)
                        }
                    }
                })
            }));

        var handler = new TextDocumentSymbolsHandler(logger, symbolFactory.Object, mockSymbolProvider.Object);
        var request = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        Assert.NotNull(result);
        var foundProperty = Assert.Single(result, symbol => symbol.DocumentSymbol?.Name == targetProperty.Name);
        Assert.NotNull(foundProperty.DocumentSymbol);
        Assert.Equal(targetProperty.StartLine, foundProperty.DocumentSymbol.Range.Start.Line); // Ensure we find the symbol at the correct line, indicating we filtered out the second reference in the target
    }

    [Fact]
    public async Task FilterDuplicateTargetPropertyOnSeparateTargetAsync()
    {
        var logger = NullLogger<TextDocumentSymbolsHandler>.Instance;

        var targetProperty1 = new Property()
        {
            Name = "TestProperty",
            StartLine = 7,
            StartChar = 14,
            EndLine = 7,
            EndChar = 44
        };
        var target1 = new Target()
        {
            Name = "TestTarget",
            StartPosition = new Position(5, 5),
            EndPosition = new Position(9, 5),
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties = [ targetProperty1 ]
                }
            ]
        };
        var targetProperty2 = new Property()
        {
            Name = targetProperty1.Name, // Must have the same name as the first target property for a valid test
            StartLine = 12,
            StartChar = 14,
            EndLine = 12,
            EndChar = 44
        };
        var target2 = new Target()
        {
            Name = "OtherTarget",
            StartPosition = new Position(10, 5),
            EndPosition = new Position(14, 5),
            PropertyGroups =
            [
                new PropertyGroup()
                {
                    Properties = [ targetProperty2 ]
                }
            ]
        };
        var fileSymbols = new Project()
        {
            Targets = [ target1 ]
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(fileSymbols);

        var symbolFactory = new Mock<ISymbolFactory>();
        symbolFactory
            .Setup(factory => factory.SymbolsForFile(It.IsAny<Project>()))
            .Returns(SymbolInformationOrDocumentSymbolContainer.From(new[]
            {
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = target1.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target1.StartPosition, target1.EndPosition),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target1.StartPosition, target1.StartPosition),
                    Children = new[]
                    {
                        new DocumentSymbol()
                        {
                            Name = targetProperty1.Name,
                            Kind = (SymbolKind)MSBuildSymbolKind.Property,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty1.StartLine, targetProperty1.StartChar, targetProperty1.EndLine, targetProperty1.EndChar),
                            SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty1.StartLine, targetProperty1.StartChar, targetProperty1.StartLine, targetProperty1.StartChar + targetProperty1.Name.Length)
                        }
                    }
                }),
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = target2.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target2.StartPosition, target2.EndPosition),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target2.StartPosition, target2.StartPosition),
                    Children = new[]
                    {
                        new DocumentSymbol()
                        {
                            Name = targetProperty2.Name,
                            Kind = (SymbolKind)MSBuildSymbolKind.Property,
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty2.StartLine, targetProperty2.StartChar, targetProperty2.EndLine, targetProperty2.EndChar),
                            SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(targetProperty2.StartLine, targetProperty2.StartChar, targetProperty2.StartLine, targetProperty2.StartChar + targetProperty2.Name.Length)
                        }
                    }
                })
            }));

        var handler = new TextDocumentSymbolsHandler(logger, symbolFactory.Object, mockSymbolProvider.Object);
        var request = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        Assert.NotNull(result);
        var foundProperty = Assert.Single(result, symbol => symbol.DocumentSymbol?.Name == targetProperty1.Name);
        Assert.NotNull(foundProperty.DocumentSymbol);
        Assert.Equal(targetProperty1.StartLine, foundProperty.DocumentSymbol.Range.Start.Line); // Ensure we find the symbol at the correct line, indicating we filtered out the reference in the second target
    }

    [Fact]
    public async Task FilterTargetRedefinitionFromSameFileAsync()
    {
        var logger = NullLogger<TextDocumentSymbolsHandler>.Instance;

        var target = new Target()
        {
            Name = "TestTarget",
            StartPosition = new Position(2, 5),
            EndPosition = new Position(3, 5)
        };
        var redefinedTarget = new Target()
        {
            Name = target.Name,
            StartPosition = new Position(4, 5),
            EndPosition = new Position(6, 5)
        };
        var fileSymbols = new Project()
        {
            Targets = [ target, redefinedTarget ]
        };

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.GetFileSymbols(It.IsAny<string>()))
            .Returns(fileSymbols);

        var symbolFactory = new Mock<ISymbolFactory>();
        symbolFactory
            .Setup(factory => factory.SymbolsForFile(It.IsAny<Project>()))
            .Returns(SymbolInformationOrDocumentSymbolContainer.From(new[]
            {
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = target.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.StartPosition, target.EndPosition),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(target.StartPosition, target.StartPosition)
                }),
                SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                {
                    Name = redefinedTarget.Name,
                    Kind = (SymbolKind)MSBuildSymbolKind.Target,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(redefinedTarget.StartPosition, redefinedTarget.EndPosition),
                    SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(redefinedTarget.StartPosition, redefinedTarget.StartPosition)
                }),
            }));

        var handler = new TextDocumentSymbolsHandler(logger, symbolFactory.Object, mockSymbolProvider.Object);
        var request = new DocumentSymbolParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        Assert.NotNull(result);
        var foundTarget = Assert.Single(result, symbol => symbol.DocumentSymbol?.Name == target.Name);
        Assert.NotNull(foundTarget.DocumentSymbol);
        Assert.Equal(target.StartPosition.Line, foundTarget.DocumentSymbol.Range.Start.Line); // Ensure we find the symbol at the correct line, indicating we filtered out the redefinition
    }
}