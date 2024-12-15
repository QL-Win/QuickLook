using System;
using System.Diagnostics;
using System.IO;

namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Represents a section of a PE image file, containing the header and a <see cref="byte" />[] representing the contents of the section.
/// </summary>
[DebuggerDisplay($"{nameof(ImageSection)}: Header = {{Header.Name,nq}}, Size: {{Data.Length}}")]
public sealed class ImageSection
{
    /// <summary>
    /// Gets the section header.
    /// </summary>
    public ImageSectionHeader Header { get; private set; }

    /// <summary>
    /// Gets a <see cref="byte" />[] representing the contents of the section.
    /// </summary>
    public byte[] Data { get; set; } = null!;

    internal ImageSection(ImageSectionHeader header)
    {
        Header = header;
    }

    public void SetDataFromRsrc(byte[] originalImage)
    {
        if (Header.PointerToRawData + Header.SizeOfRawData <= originalImage.Length)
        {
            Data = originalImage.GetBytes((int)Header.PointerToRawData, (int)Header.SizeOfRawData);
        }
        else
        {
            throw new PEImageParseException(int.MinValue, "Section '" + Header.Name + "' incomplete.");
        }
    }

    public void SetDataFromRsrc(Stream originalImage, uint? length = null)
    {
        if (Header.PointerToRawData + Header.SizeOfRawData <= (length ?? originalImage.Length))
        {
            Data = originalImage.GetBytes((int)Header.PointerToRawData, (int)Header.SizeOfRawData);
        }
        else
        {
            throw new PEImageParseException(int.MinValue, "Section '" + Header.Name + "' incomplete.");
        }
    }
}

/// <summary>
/// Provides support for creation and generation of generic objects.
/// </summary>
file static class Create
{
    /// <summary>
    /// Copies a specified number of bytes from this <see cref="byte" />[] and returns a new array representing a fraction of the original <see cref="byte" />[].
    /// </summary>
    /// <param name="array">The <see cref="byte" />[] to take the subset of bytes from.</param>
    /// <param name="index">A <see cref="int" /> value specifying the offset from which to start copying bytes.</param>
    /// <param name="count">A <see cref="int" /> value specifying the number of bytes to copy.</param>
    /// <returns>
    /// A new <see cref="byte" />[] representing a fraction of the original <see cref="byte" />[].
    /// </returns>
    public static byte[] GetBytes(this byte[] array, int index, int count)
    {
        byte[] result = new byte[count];
        Buffer.BlockCopy(array, index, result, 0, count);
        return result;
    }

    public static byte[] GetBytes(this Stream stream, long index, int count)
    {
        byte[] buffer = new byte[count];

        // Ensure the stream is at the correct position
        long currentPosition = stream.Position;
        long bytesToSkip = index - currentPosition;

        if (bytesToSkip < 0)
        {
            throw new ArgumentException("Offset is before the current position in the stream.");
        }

        // Skip bytes until reaching the target offset
        while (bytesToSkip > 0)
        {
            int bytesSkipped = (int)Math.Min(bytesToSkip, int.MaxValue);
            stream.Read(new byte[bytesSkipped], 0, bytesSkipped);
            bytesToSkip -= bytesSkipped;
        }

        // Read the required number of bytes
        int bytesRead = stream.Read(buffer, 0, count);
        if (bytesRead < count)
        {
            throw new EndOfStreamException("The stream has fewer bytes available than requested.");
        }

        return buffer;
    }
}
