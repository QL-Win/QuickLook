using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Represents a PE (x86) or a PE+ (x64) image. This class parses binary files, typically EXE and DLL files.
/// </summary>
public class PEImage
{
    /// <summary>
    /// Gets the original PE image file, if this file was loaded from an existing source; otherwise, <see langword="null" />.
    /// </summary>
    public byte[] OriginalImage { get; private set; }

    /// <summary>
    /// Gets the DOS header of this PE image file.
    /// </summary>
    public ImageDosHeader DosHeader { get; private set; }

    /// <summary>
    /// Gets the MS-DOS stub of this PE image file.
    /// </summary>
    public byte[] DosStub { get; private set; }

    /// <summary>
    /// Gets the COFF header of this PE image file.
    /// </summary>
    public ImageCoffHeader CoffHeader { get; private set; }

    /// <summary>
    /// Gets the optional header of this PE image file.
    /// </summary>
    public ImageOptionalHeader OptionalHeader { get; private set; }

    /// <summary>
    /// Gets the collection of section headers and data of this PE image file.
    /// </summary>
    public ImageSection[] Sections { get; private set; }

    private PEImage(byte[] originalImage)
    {
        OriginalImage = originalImage;

        using BinaryReader reader = new(new MemoryStream(OriginalImage));

        // MZ
        if (reader.BaseStream.Length < 2) throw new PEImageParseException(0, "DOS signature not found.");
        if (reader.ReadUInt16() != 0x5a4d) throw new PEImageParseException(0, "DOS header not found.");

        // DOS Header
        if (reader.BaseStream.Length - reader.BaseStream.Position < 64) throw new PEImageParseException((int)reader.BaseStream.Position, "DOS header incomplete.");

        DosHeader = new()
        {
            LastPageSize = reader.ReadUInt16(),
            PageCount = reader.ReadUInt16(),
            RelocationCount = reader.ReadUInt16(),
            HeaderSize = reader.ReadUInt16(),
            MinAlloc = reader.ReadUInt16(),
            MaxAlloc = reader.ReadUInt16(),
            InitialSS = reader.ReadUInt16(),
            InitialSP = reader.ReadUInt16(),
            Checksum = reader.ReadUInt16(),
            InitialIP = reader.ReadUInt16(),
            InitialCS = reader.ReadUInt16(),
            RelocationOffset = reader.ReadUInt16(),
            OverlayNumber = reader.ReadUInt16(),
            Reserved1 = reader.ReadUInt16(),
            Reserved2 = reader.ReadUInt16(),
            Reserved3 = reader.ReadUInt16(),
            Reserved4 = reader.ReadUInt16(),
            OemIdentifier = reader.ReadUInt16(),
            OemInformation = reader.ReadUInt16(),
            Reserved5 = reader.ReadUInt16(),
            Reserved6 = reader.ReadUInt16(),
            Reserved7 = reader.ReadUInt16(),
            Reserved8 = reader.ReadUInt16(),
            Reserved9 = reader.ReadUInt16(),
            Reserved10 = reader.ReadUInt16(),
            Reserved11 = reader.ReadUInt16(),
            Reserved12 = reader.ReadUInt16(),
            Reserved13 = reader.ReadUInt16(),
            Reserved14 = reader.ReadUInt16(),
            PEHeaderOffset = reader.ReadUInt32()
        };

        // DOS Stub
        if (reader.BaseStream.Length < DosHeader.PEHeaderOffset) throw new PEImageParseException((int)reader.BaseStream.Position, "DOS stub incomplete.");

        DosStub = reader.ReadBytes((int)(DosHeader.PEHeaderOffset - reader.BaseStream.Position));

        // COFF Header
        if (reader.ReadUInt32() != 0x4550) throw new PEImageParseException((int)reader.BaseStream.Position - 4, "COFF header not found.");
        if (reader.BaseStream.Length - reader.BaseStream.Position < 20) throw new PEImageParseException((int)reader.BaseStream.Position, "COFF header incomplete.");

        CoffHeader = new()
        {
            Machine = (ImageMachineType)reader.ReadUInt16(),
            NumberOfSections = reader.ReadUInt16(),
            TimeDateStamp = reader.ReadUInt32(),
            PointerToSymbolTable = reader.ReadUInt32(),
            NumberOfSymbols = reader.ReadUInt32(),
            SizeOfOptionalHeader = reader.ReadUInt16(),
            Characteristics = (ImageCharacteristics)reader.ReadUInt16()
        };

        // Optional Header
        if (reader.BaseStream.Length - reader.BaseStream.Position < 2) throw new PEImageParseException((int)reader.BaseStream.Position, "Optional header not found.");
        ushort magic = reader.ReadUInt16();

        if (magic == 0x10b)
        {
            if (reader.BaseStream.Length - reader.BaseStream.Position < 94) throw new PEImageParseException((int)reader.BaseStream.Position, "Optional header incomplete.");

            OptionalHeader = new ImageOptionalHeader32
            {
                MajorLinkerVersion = reader.ReadByte(),
                MinorLinkerVersion = reader.ReadByte(),
                SizeOfCode = reader.ReadUInt32(),
                SizeOfInitializedData = reader.ReadUInt32(),
                SizeOfUninitializedData = reader.ReadUInt32(),
                AddressOfEntryPoint = reader.ReadUInt32(),
                BaseOfCode = reader.ReadUInt32(),
                BaseOfData = reader.ReadUInt32(),
                ImageBase = reader.ReadUInt32(),
                SectionAlignment = reader.ReadUInt32(),
                FileAlignment = reader.ReadUInt32(),
                MajorOperatingSystemVersion = reader.ReadUInt16(),
                MinorOperatingSystemVersion = reader.ReadUInt16(),
                MajorImageVersion = reader.ReadUInt16(),
                MinorImageVersion = reader.ReadUInt16(),
                MajorSubsystemVersion = reader.ReadUInt16(),
                MinorSubsystemVersion = reader.ReadUInt16(),
                Win32VersionValue = reader.ReadUInt32(),
                SizeOfImage = reader.ReadUInt32(),
                SizeOfHeaders = reader.ReadUInt32(),
                Checksum = reader.ReadUInt32(),
                Subsystem = (ImageSubsystem)reader.ReadUInt16(),
                DllCharacteristics = (ImageDllCharacteristics)reader.ReadUInt16(),
                SizeOfStackReserve = reader.ReadUInt32(),
                SizeOfStackCommit = reader.ReadUInt32(),
                SizeOfHeapReserve = reader.ReadUInt32(),
                SizeOfHeapCommit = reader.ReadUInt32(),
                LoaderFlags = reader.ReadUInt32(),
                NumberOfRvaAndSizes = reader.ReadUInt32()
            };
        }
        else if (magic == 0x20b)
        {
            if (reader.BaseStream.Length - reader.BaseStream.Position < 110) throw new PEImageParseException((int)reader.BaseStream.Position, "Optional header incomplete.");

            OptionalHeader = new ImageOptionalHeader64
            {
                MajorLinkerVersion = reader.ReadByte(),
                MinorLinkerVersion = reader.ReadByte(),
                SizeOfCode = reader.ReadUInt32(),
                SizeOfInitializedData = reader.ReadUInt32(),
                SizeOfUninitializedData = reader.ReadUInt32(),
                AddressOfEntryPoint = reader.ReadUInt32(),
                BaseOfCode = reader.ReadUInt32(),
                ImageBase = reader.ReadUInt64(),
                SectionAlignment = reader.ReadUInt32(),
                FileAlignment = reader.ReadUInt32(),
                MajorOperatingSystemVersion = reader.ReadUInt16(),
                MinorOperatingSystemVersion = reader.ReadUInt16(),
                MajorImageVersion = reader.ReadUInt16(),
                MinorImageVersion = reader.ReadUInt16(),
                MajorSubsystemVersion = reader.ReadUInt16(),
                MinorSubsystemVersion = reader.ReadUInt16(),
                Win32VersionValue = reader.ReadUInt32(),
                SizeOfImage = reader.ReadUInt32(),
                SizeOfHeaders = reader.ReadUInt32(),
                Checksum = reader.ReadUInt32(),
                Subsystem = (ImageSubsystem)reader.ReadUInt16(),
                DllCharacteristics = (ImageDllCharacteristics)reader.ReadUInt16(),
                SizeOfStackReserve = reader.ReadUInt64(),
                SizeOfStackCommit = reader.ReadUInt64(),
                SizeOfHeapReserve = reader.ReadUInt64(),
                SizeOfHeapCommit = reader.ReadUInt64(),
                LoaderFlags = reader.ReadUInt32(),
                NumberOfRvaAndSizes = reader.ReadUInt32()
            };
        }
        else if (magic == 0x107)
        {
            throw new PEImageParseException((int)reader.BaseStream.Position - 2, "Optional header for ROM's is not supported.");
        }
        else
        {
            throw new PEImageParseException((int)reader.BaseStream.Position - 2, "Optional header magic value of '0x" + magic.ToString("x4") + "' unknown.");
        }

        // Data Directories
        if (reader.BaseStream.Length - reader.BaseStream.Position < OptionalHeader.NumberOfRvaAndSizes * 8) throw new PEImageParseException((int)reader.BaseStream.Position, "Data directories incomplete.");

        OptionalHeader.DataDirectories = Create.Array((int)OptionalHeader.NumberOfRvaAndSizes, i => new ImageDataDirectory((ImageDataDirectoryName)i, reader.ReadUInt32(), reader.ReadUInt32()));

        // Section Headers
        if (reader.BaseStream.Length - reader.BaseStream.Position < CoffHeader.NumberOfSections * 40) throw new PEImageParseException((int)reader.BaseStream.Position, "Section headers incomplete.");

        Sections = Create
            .Enumerable(CoffHeader.NumberOfSections, i => new ImageSectionHeader
            {
                Name = reader.ReadBytes(8).TakeWhile(c => c != 0).ToArray().ToUTF8String(),
                VirtualSize = reader.ReadUInt32(),
                VirtualAddress = reader.ReadUInt32(),
                SizeOfRawData = reader.ReadUInt32(),
                PointerToRawData = reader.ReadUInt32(),
                PointerToRelocations = reader.ReadUInt32(),
                PointerToLineNumbers = reader.ReadUInt32(),
                NumberOfRelocations = reader.ReadUInt16(),
                NumberOfLineNumbers = reader.ReadUInt16(),
                Characteristics = (ImageSectionFlags)reader.ReadUInt32()
            })
            .Select(header =>
            {
                return new ImageSection(header);

                //if (header.PointerToRawData + header.SizeOfRawData <= reader.BaseStream.Length)
                //{
                //    return new ImageSection(header, OriginalImage.GetBytes((int)header.PointerToRawData, (int)header.SizeOfRawData));
                //}
                //else
                //{
                //    throw new PEImageParseException((int)reader.BaseStream.Position, "Section '" + header.Name + "' incomplete.");
                //}
            })
            .ToArray();
    }

    /// <summary>
    /// Creates a <see cref="PEImage" /> from the specified file with the specified form name.
    /// </summary>
    /// <param name="path">A <see cref="string" /> specifying the path of a file from which to create the <see cref="PEImage" />.</param>
    /// <returns>
    /// The <see cref="PEImage" /> this method creates.
    /// </returns>
    public static PEImage FromFile(string path)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));
        _ = File.Exists(path) ? default(bool) : throw new FileNotFoundException(path);

        return new(File.ReadAllBytes(path));
    }

    /// <summary>
    /// Creates a <see cref="PEImage" /> from the specified <see cref="byte" />[] that represents a PE image file.
    /// </summary>
    /// <param name="file">The <see cref="byte" />[] that represents a <see cref="PEImage" /> to parse.</param>
    /// <returns>
    /// The <see cref="PEImage" /> this method creates.
    /// </returns>
    public static PEImage FromBinary(byte[] file)
    {
        _ = file ?? throw new ArgumentNullException(nameof(file));

        return new([.. file]);
    }
}

/// <summary>
/// Provides support for creation and generation of generic objects.
/// </summary>
file static class Create
{
    /// <summary>
    /// Creates an <see cref="System.Array" /> of the specified type and initialized each element with a value that is retrieved from <paramref name="valueSelector" />.
    /// </summary>
    /// <typeparam name="T">The type of the created <see cref="System.Array" />.</typeparam>
    /// <param name="length">The number of elements of the <see cref="System.Array" />.</param>
    /// <param name="valueSelector">A <see cref="Func{T, TResult}" /> to retrieve new values for the <see cref="System.Array" /> based on the given index.</param>
    /// <returns>
    /// A new <see cref="System.Array" /> with the specified length, where each element is initialized with a value that is retrieved from <paramref name="valueSelector" />.
    /// </returns>
    public static T[] Array<T>(int length, Func<int, T> valueSelector)
    {
        T[] array = new T[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = valueSelector(i);
        }
        return array;
    }

    /// <summary>
    /// Creates an <see cref="IEnumerable{T}" /> of the specified type and returns values that are retrieved from <paramref name="valueSelector" />.
    /// </summary>
    /// <typeparam name="T">The type of the created <see cref="IEnumerable{T}" />.</typeparam>
    /// <param name="count">The number of elements to return.</param>
    /// <param name="valueSelector">A <see cref="Func{T, TResult}" /> to retrieve new values for the <see cref="IEnumerable{T}" /> based on the given index.</param>
    /// <returns>
    /// A new <see cref="IEnumerable{T}" /> with the specified number of elements, where each element is initialized with a value that is retrieved from <paramref name="valueSelector" />.
    /// </returns>
    public static IEnumerable<T> Enumerable<T>(int count, Func<int, T> valueSelector)
    {
        for (int i = 0; i < count; i++)
        {
            yield return valueSelector(i);
        }
    }

    /// <summary>
    /// Decodes all the bytes in this <see cref="byte" />[] into a <see cref="string" /> using the <see cref="Encoding.UTF8" /> encoding.
    /// </summary>
    /// <param name="array">The <see cref="byte" />[] containing the sequence of bytes to decode.</param>
    /// <returns>
    /// A <see cref="string" /> that contains the results of decoding this sequence of bytes.
    /// </returns>
    public static string ToUTF8String(this byte[] array)
    {
        return Encoding.UTF8.GetString(array);
    }

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
}
