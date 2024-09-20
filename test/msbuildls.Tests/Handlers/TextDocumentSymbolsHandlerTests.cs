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
                        Kind = (SymbolKind)MsBuildSymbolKind.Property,
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
                        Kind = (SymbolKind)MsBuildSymbolKind.Property,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property1.StartLine, property1.StartChar, property1.EndLine, property1.EndChar),
                        SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(property1.StartLine, property1.StartChar, property1.StartLine, property1.StartChar + property1.Name.Length)
                    }),
                    SymbolInformationOrDocumentSymbol.Create(new DocumentSymbol()
                    {
                        Name = property2.Name,
                        Kind = (SymbolKind)MsBuildSymbolKind.Property,
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
}