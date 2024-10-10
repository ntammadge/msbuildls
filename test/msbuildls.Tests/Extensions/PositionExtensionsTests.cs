using msbuildls.LanguageServer.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace msbuildls.Tests.Extensions;

public class PositionExtensionTests
{
    [Theory]
    [InlineData(1, 2, 1, 1, 1, 3, true)] // Single-line range, between ends
    [InlineData(1, 1, 1, 1, 1, 3, true)] // Single-line range, at start position
    [InlineData(1, 3, 1, 1, 1, 3, true)] // Single-line range, at end position
    [InlineData(1, 1, 1, 1, 1, 1, true)] // 0 length range, at both end positions
    [InlineData(1, 0, 1, 1, 1, 3, false)] // Single-line range, before start character
    [InlineData(1, 4, 1, 1, 1, 3, false)] // Single-line range, after end character
    [InlineData(0, 1, 1, 1, 1, 3, false)] // Single-line range, before start line
    [InlineData(2, 1, 1, 1, 1, 3, false)] // Single-line range, after end line
    [InlineData(1, 2, 1, 1, 2, 3, true)] // Multi-line range, at start line, after start position
    [InlineData(1, 1, 1, 1, 2, 3, true)] // Multi-line range, at start line, at start position
    [InlineData(2, 2, 1, 1, 2, 3, true)] // Multi-line range, at end line, before end position
    [InlineData(2, 3, 1, 1, 2, 3, true)] // Multi-line range, at end line, at end position
    [InlineData(2, 1, 1, 1, 3, 3, true)] // Multi-line range, between start and end lines
    [InlineData(1, 0, 1, 1, 2, 3, false)] // Multi-line range, at start line, before start position
    [InlineData(2, 4, 1, 1, 2, 3, false)] // Multi-line range, at end line, after end position
    [InlineData(0, 1, 1, 1, 2, 3, false)] // Multi-line range, before start line
    [InlineData(3, 0, 1, 1, 2, 3, false)] // Multi-line range, after end line
    public void CanIdentifyIfPositionIsInRange(int posLine, int posChar, int rangeStartLine, int rangeStartChar, int rangeEndLine, int rangeEndChar, bool expectedInRange)
    {
        var position = new Position(posLine, posChar);
        var range = new Range(rangeStartLine, rangeStartChar, rangeEndLine, rangeEndChar);
        Assert.Equal(expectedInRange, position.IsIn(range));
    }
}