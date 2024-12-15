using System;

namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// The exception that is thrown when parsing of a <see cref="PEImage" /> fails.
/// </summary>
public sealed class PEImageParseException : Exception
{
    /// <summary>
    /// Gets the offset within the image file at which parsing failed.
    /// </summary>
    public int Offset { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PEImageParseException" /> class.
    /// </summary>
    /// <param name="offset">The offset within the image file at which parsing failed.</param>
    /// <param name="message">The message that describes the error.</param>
    public PEImageParseException(int offset, string message) : base(message)
    {
        _ = message ?? throw new ArgumentNullException(nameof(message));

        Offset = offset;
    }
}
