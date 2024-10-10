using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace msbuildls.LanguageServer.Extensions;

public static class PositionExtensions
{
    /// <summary>
    /// Determines if the position is within the specified range
    /// </summary>
    /// <param name="position"></param>
    /// <param name="range"></param>
    /// <returns>True if the position is within the range. False otherwise</returns>
    public static bool IsIn(this Position position, Range range)
    {
        // Return true if the position is at or after the character at the beginning of the range and at or before the character at the end of the range
        return (range.Start.Line == range.End.Line && position.Line == range.Start.Line && position.Character >= range.Start.Character && position.Character <= range.End.Character)
            || (range.Start.Line < range.End.Line
                && ((position.Line == range.Start.Line && position.Character >= range.Start.Character)
                    || (position.Line == range.End.Line && position.Character <= range.End.Character)
                    || (position.Line > range.Start.Line && position.Line < range.End.Line)));
    }
}