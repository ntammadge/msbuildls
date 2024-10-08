using System;
using System.IO;
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

public class TextDocumentHandlerTests
{
    [Fact]
    public async Task OpenDocumentAddsSymbolsToProviderAsync()
    {
        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\SomeFile.targets")
            }
        };
        var factorySymbols = new Project();

        var nullLogger = NullLogger<TextDocumentHandler>.Instance;

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Returns(factorySymbols);

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        mockSymbolProvider
            .Setup(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()))
            .Callback<string, Project>((fileName, fileSymbols) =>
            {
                Assert.Equal(request.TextDocument.Uri, fileName);
                Assert.Equal(factorySymbols, fileSymbols);
            });

        var handler = new TextDocumentHandler(
            nullLogger,
            mockSymbolFactory.Object,
            mockSymbolProvider.Object
            );



        await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Once);
    }

    [Fact]
    public async Task OpenDocumentDoesNotAddNullEntryIfParsingErrorAsync()
    {
        var nullLogger = NullLogger<TextDocumentHandler>.Instance;
        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Returns((Project?)null);
        var mockSymbolProvider = new Mock<ISymbolProvider>();

        var handler = new TextDocumentHandler(
            nullLogger,
            mockSymbolFactory.Object,
            mockSymbolProvider.Object
            );

        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(@"C:\DoesNotExist.targets")
            }
        };

        await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task OpenDocumentDoesDeserializeImportedFilesWithFullyQualifiedPathsAsync()
    {
        var openedFilePath = GetFullyQualifiedTestDataFilePath("opened.targets");
        var importedFilePath = GetFullyQualifiedTestDataFilePath("imported.targets");

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Callback<TextDocumentItem>((textDocument) =>
            {
                Assert.NotNull(textDocument);
                Assert.Equal(openedFilePath, textDocument.Uri.ToUri().LocalPath);
            })
            .Returns(new Project()
            {
                Imports = [
                    new Import()
                    {
                        Project = importedFilePath
                    }
                ]
            });
        mockSymbolFactory
            .Setup(factory => factory.ParseFile(It.IsAny<string>()))
            .Callback<string>((filePath) =>
            {
                Assert.Equal(importedFilePath, filePath);
            })
            .Returns(new Project());

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        var handler = new TextDocumentHandler(NullLogger<TextDocumentHandler>.Instance, mockSymbolFactory.Object, mockSymbolProvider.Object);

        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(openedFilePath),
                Text = string.Empty // Real content is unnecessary because the symbol factory is mocked
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolFactory.Verify(factory => factory.ParseFile(It.IsAny<string>()), Times.Once);
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Exactly(2));
    }

    [Fact]
    public async Task OpenDocumentDoesDeserializeImportedFilesWithRelativePathsAsync()
    {
        var openedFilePath = GetFullyQualifiedTestDataFilePath("opened.targets");

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Callback<TextDocumentItem>((textDocument) =>
            {
                Assert.NotNull(textDocument);
                Assert.Equal(openedFilePath, textDocument.Uri.ToUri().LocalPath);
            })
            .Returns(new Project()
            {
                Imports = [
                    new Import()
                    {
                        Project = @".\imported.targets"
                    }
                ]
            });
        mockSymbolFactory
            .Setup(factory => factory.ParseFile(It.IsAny<string>()))
            .Callback<string>((filePath) =>
            {
                Assert.Equal(GetFullyQualifiedTestDataFilePath("imported.targets"), filePath);
            })
            .Returns(new Project());

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        var handler = new TextDocumentHandler(NullLogger<TextDocumentHandler>.Instance, mockSymbolFactory.Object, mockSymbolProvider.Object);

        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(openedFilePath),
                Text = string.Empty // Real content is unnecessary because the symbol factory is mocked
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolFactory.Verify(factory => factory.ParseFile(It.IsAny<string>()), Times.Once);
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Exactly(2));
    }

    [Fact]
    public async Task OpenDocumentDoesDeserializeImportedFilesFromImportedFilesAsync()
    {
        var openedFilePath = GetFullyQualifiedTestDataFilePath("opened.targets");
        var importedFilePath = "imported.targets";
        var nestedImportedFilePath = "nestedImport.targets";

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Callback<TextDocumentItem>((textDocument) =>
            {
                Assert.NotNull(textDocument);
                Assert.Equal(openedFilePath, textDocument.Uri.ToUri().LocalPath);
            })
            .Returns(new Project()
            {
                Imports = [
                    new Import()
                    {
                        Project = importedFilePath
                    }
                ]
            });
        mockSymbolFactory
            .Setup(factory => factory.ParseFile(It.Is<string>((filePath) => filePath == GetFullyQualifiedTestDataFilePath(importedFilePath))))
            .Returns(new Project()
            {
                Imports = [
                    new Import()
                    {
                        Project = nestedImportedFilePath
                    }
                ]
            });
        mockSymbolFactory
            .Setup(factory => factory.ParseFile(It.Is<string>((filePath) => filePath == GetFullyQualifiedTestDataFilePath(nestedImportedFilePath))))
            .Returns(new Project());

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        var handler = new TextDocumentHandler(NullLogger<TextDocumentHandler>.Instance, mockSymbolFactory.Object, mockSymbolProvider.Object);

        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(openedFilePath),
                Text = string.Empty // Real content is unnecessary because the symbol factory is mocked
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolFactory.Verify(factory => factory.ParseFile(It.IsAny<string>()), Times.Exactly(2));
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Exactly(3));
    }

    [Fact]
    public async Task OpenDocumentDoesNotInfiniteLoopIfDocumentAttemptsToImportItselfAsync()
    {
        var openedFilePath = @"C:\Opened\targets.targets";

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Callback<TextDocumentItem>((textDocument) =>
            {
                Assert.NotNull(textDocument);
                Assert.Equal(openedFilePath, textDocument.Uri.ToUri().LocalPath);
            })
            .Returns(new Project()
            {
                Imports = [
                    new Import()
                    {
                        Project = openedFilePath // Intentionally the same path in order to test self-reference/infinite loop avoidance
                    }
                ]
            });

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        var handler = new TextDocumentHandler(NullLogger<TextDocumentHandler>.Instance, mockSymbolFactory.Object, mockSymbolProvider.Object);

        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(openedFilePath),
                Text = string.Empty // Real content is unnecessary because the symbol factory is mocked
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolFactory.Verify(factory => factory.ParseFile(It.IsAny<string>()), Times.Never);
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Once);
    }

    [Fact]
    public async Task OpenDocumentDeserializesImportFromImportGroupsAsync()
    {
        var openedFilePath = GetFullyQualifiedTestDataFilePath("opened.targets");
        var importedFilePath = GetFullyQualifiedTestDataFilePath("imported.targets");

        var mockSymbolFactory = new Mock<ISymbolFactory>();
        mockSymbolFactory
            .Setup(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()))
            .Callback<TextDocumentItem>((textDocument) =>
            {
                Assert.NotNull(textDocument);
                Assert.Equal(openedFilePath, textDocument.Uri.ToUri().LocalPath);
            })
            .Returns(new Project()
            {
                ImportGroups = [
                    new ImportGroup()
                    {
                        Imports = [
                            new Import()
                            {
                                Project = importedFilePath
                            }
                        ]
                    }
                ]
            });
        mockSymbolFactory
            .Setup(factory => factory.ParseFile(It.IsAny<string>()))
            .Callback<string>((filePath) =>
            {
                Assert.Equal(importedFilePath, filePath);
            })
            .Returns(new Project());

        var mockSymbolProvider = new Mock<ISymbolProvider>();
        var handler = new TextDocumentHandler(NullLogger<TextDocumentHandler>.Instance, mockSymbolFactory.Object, mockSymbolProvider.Object);

        var request = new DidOpenTextDocumentParams()
        {
            TextDocument = new TextDocumentItem()
            {
                Uri = new Uri(openedFilePath),
                Text = string.Empty // Real content is unnecessary because the symbol factory is mocked
            }
        };

        var result = await handler.Handle(request, new CancellationToken());

        mockSymbolFactory.Verify(factory => factory.ParseDocument(It.IsAny<TextDocumentItem>()), Times.Once);
        mockSymbolFactory.Verify(factory => factory.ParseFile(It.IsAny<string>()), Times.Once);
        mockSymbolProvider.Verify(provider => provider.AddOrUpdateSymbols(It.IsAny<string>(), It.IsAny<Project>()), Times.Exactly(2));
    }

    private string GetFullyQualifiedTestDataFilePath(string fileName)
    {
        var relativeDirectory = @"..\..\..\..\testData\TextDocumentHandlerTests";
        return Path.GetFullPath(Path.Combine(relativeDirectory, fileName));
    }
}