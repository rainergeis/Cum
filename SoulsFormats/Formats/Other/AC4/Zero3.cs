﻿using System.Collections.Generic;
using System.IO;

namespace SoulsFormats.AC4;

/// <summary>
///     A multi-file container format used in Armored Core 4.
/// </summary>
public class Zero3
{

    internal Zero3(BinaryReaderEx br, List<BinaryReaderEx> containers)
    {
        br.BigEndian = true;

        var fileCount = br.ReadInt32();
        br.AssertInt32(0x10);
        br.AssertInt32(0x10);
        br.AssertInt32(0x800000); // Max file size (8 MB)
        br.AssertPattern(0x40, 0x00);

        Files = new List<File>(fileCount);
        for (var i = 0; i < fileCount; i++)
            Files.Add(new File(br, containers));
    }

    /// <summary>
    ///     Files in this container.
    /// </summary>
    public List<File> Files { get; }

    /// <summary>
    ///     Returns whether the file appears to be a .000 header.
    /// </summary>
    public static bool Is(string path)
    {
        using (FileStream fs = System.IO.File.OpenRead(path))
        {
            var br = new BinaryReaderEx(true, fs);
            if (br.Length < 0x50 || br.GetInt32(4) != 0x10 || br.GetInt32(8) != 0x10 || br.GetInt32(0xC) != 0x800000)
                return false;

            for (var i = 0; i < 16; i++)
                if (br.GetInt32(0x10 + i * 4) != 0)
                    return false;
            return true;
        }
    }

    /// <summary>
    ///     Read file data from the given .000 header file and associated data files.
    /// </summary>
    public static Zero3 Read(string path)
    {
        var containers = new List<BinaryReaderEx>();
        var index = 0;
        var containerPath = Path.ChangeExtension(path, index.ToString("D3"));
        while (System.IO.File.Exists(containerPath))
        {
            containers.Add(new BinaryReaderEx(true, System.IO.File.OpenRead(containerPath)));
            index++;
            containerPath = Path.ChangeExtension(path, index.ToString("D3"));
        }

        var result = new Zero3(containers[0], containers);
        foreach (BinaryReaderEx br in containers)
            br.Stream.Close();
        return result;
    }

    /// <summary>
    ///     A generic file in a Zero3 container.
    /// </summary>
    public class File
    {

        internal File(BinaryReaderEx br, List<BinaryReaderEx> containers)
        {
            Name = br.ReadFixStr(0x40);
            var containerIndex = br.ReadInt32();
            var fileOffset = br.ReadUInt32(); // Multiply by 0x10
            br.ReadInt32(); // Padded file size; multiply by 0x10
            var fileSize = br.ReadInt32();

            Bytes = containers[containerIndex].GetBytes(fileOffset * 0x10, fileSize);
        }

        /// <summary>
        ///     Name of the file; maximum 0x40 characters.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Raw file data.
        /// </summary>
        public byte[] Bytes { get; }
    }
}