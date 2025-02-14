﻿using System;
using System.Collections.Generic;
using System.IO;

namespace SoulsFormats;

/// <summary>
///     A general-purpose headered file container used in DS1 and DSR. Extensions: .*bhd (header) and .*bdt (data)
/// </summary>
public class BXF3 : IBinder, IBXF3
{

    /// <summary>
    ///     Creates an empty BXF3 formatted for DS1.
    /// </summary>
    public BXF3()
    {
        Files = new List<BinderFile>();
        Version = SFUtil.DateToBinderTimestamp(DateTime.Now);
        Format = Binder.Format.IDs | Binder.Format.Names1 | Binder.Format.Names2 | Binder.Format.Compression;
    }

    private BXF3(BinaryReaderEx bhdReader, BinaryReaderEx bdtReader)
    {
        ReadBDFHeader(bdtReader);
        List<BinderFileHeader> fileHeaders = ReadBHFHeader(this, bhdReader);
        Files = new List<BinderFile>(fileHeaders.Count);
        foreach (BinderFileHeader fileHeader in fileHeaders)
            Files.Add(fileHeader.ReadFileData(bdtReader));
    }

    /// <summary>
    ///     The files contained within this BXF3.
    /// </summary>
    public List<BinderFile> Files { get; set; }

    /// <summary>
    ///     A timestamp or version number, 8 characters maximum.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    ///     Indicates the format of this BXF3.
    /// </summary>
    public Binder.Format Format { get; set; }

    /// <summary>
    ///     Write file in big-endian mode for PS3/X360.
    /// </summary>
    public bool BigEndian { get; set; }

    /// <summary>
    ///     Controls ordering of flag bits.
    /// </summary>
    public bool BitBigEndian { get; set; }

    private static bool IsBHD(BinaryReaderEx br)
    {
        if (br.Length < 4)
            return false;

        var magic = br.GetASCII(0, 4);
        return magic == "BHF3";
    }

    private static bool IsBDT(BinaryReaderEx br)
    {
        if (br.Length < 4)
            return false;

        var magic = br.GetASCII(0, 4);
        return magic == "BDF3";
    }

    internal static void ReadBDFHeader(BinaryReaderEx br)
    {
        br.AssertASCII("BDF3");
        br.ReadFixStr(8); // Version
        br.AssertInt32(0);
    }

    internal static List<BinderFileHeader> ReadBHFHeader(IBXF3 bxf, BinaryReaderEx br)
    {
        br.AssertASCII("BHF3");
        bxf.Version = br.ReadFixStr(8);

        bxf.BitBigEndian = br.GetBoolean(0xE);

        bxf.Format = Binder.ReadFormat(br, bxf.BitBigEndian);
        bxf.BigEndian = br.ReadBoolean();
        br.AssertBoolean(bxf.BitBigEndian);
        br.AssertByte(0);

        br.BigEndian = bxf.BigEndian || Binder.ForceBigEndian(bxf.Format);

        var fileCount = br.ReadInt32();
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0);

        var fileHeaders = new List<BinderFileHeader>(fileCount);
        for (var i = 0; i < fileCount; i++)
            fileHeaders.Add(BinderFileHeader.ReadBinder3FileHeader(br, bxf.Format, bxf.BitBigEndian));

        return fileHeaders;
    }

    private void Write(BinaryWriterEx bhdWriter, BinaryWriterEx bdtWriter)
    {
        var fileHeaders = new List<BinderFileHeader>(Files.Count);
        foreach (BinderFile file in Files)
            fileHeaders.Add(new BinderFileHeader(file));

        WriteBDFHeader(this, bdtWriter);
        WriteBHFHeader(this, bhdWriter, fileHeaders);
        for (var i = 0; i < Files.Count; i++)
            fileHeaders[i].WriteBinder3FileData(bhdWriter, bdtWriter, Format, i, Files[i].Bytes);
    }

    internal static void WriteBDFHeader(IBXF3 bxf, BinaryWriterEx bw)
    {
        bw.WriteASCII("BDF3");
        bw.WriteFixStr(bxf.Version, 8);
        bw.WriteInt32(0);
    }

    internal static void WriteBHFHeader(IBXF3 bxf, BinaryWriterEx bw, List<BinderFileHeader> fileHeaders)
    {
        bw.BigEndian = bxf.BigEndian || Binder.ForceBigEndian(bxf.Format);

        bw.WriteASCII("BHF3");
        bw.WriteFixStr(bxf.Version, 8);

        Binder.WriteFormat(bw, bxf.BitBigEndian, bxf.Format);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteByte(0);

        bw.WriteInt32(fileHeaders.Count);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);

        for (var i = 0; i < fileHeaders.Count; i++)
            fileHeaders[i].WriteBinder3FileHeader(bw, bxf.Format, bxf.BitBigEndian, i);

        for (var i = 0; i < fileHeaders.Count; i++)
            fileHeaders[i].WriteFileName(bw, bxf.Format, false, i);
    }

    #region Public Is

    /// <summary>
    ///     Returns true if the bytes appear to be a BXF3 header file.
    /// </summary>
    public static bool IsBHD(byte[] bytes)
    {
        var br = new BinaryReaderEx(false, bytes);
        return IsBHD(SFUtil.GetDecompressedBR(br, out _));
    }

    /// <summary>
    ///     Returns true if the file appears to be a BXF3 header file.
    /// </summary>
    public static bool IsBHD(string path)
    {
        using (FileStream fs = File.OpenRead(path))
        {
            var br = new BinaryReaderEx(false, fs);
            return IsBHD(SFUtil.GetDecompressedBR(br, out _));
        }
    }

    /// <summary>
    ///     Returns true if the file appears to be a BXF3 data file.
    /// </summary>
    public static bool IsBDT(byte[] bytes)
    {
        var br = new BinaryReaderEx(false, bytes);
        return IsBDT(SFUtil.GetDecompressedBR(br, out _));
    }

    /// <summary>
    ///     Returns true if the file appears to be a BXF3 data file.
    /// </summary>
    public static bool IsBDT(string path)
    {
        using (FileStream fs = File.OpenRead(path))
        {
            var br = new BinaryReaderEx(false, fs);
            return IsBDT(SFUtil.GetDecompressedBR(br, out _));
        }
    }

    #endregion

    #region Public Read

    /// <summary>
    ///     Reads two arrays of bytes as the BHD and BDT.
    /// </summary>
    public static BXF3 Read(byte[] bhdBytes, byte[] bdtBytes)
    {
        var bhdReader = new BinaryReaderEx(false, bhdBytes);
        var bdtReader = new BinaryReaderEx(false, bdtBytes);
        return new BXF3(bhdReader, bdtReader);
    }

    /// <summary>
    ///     Reads an array of bytes as the BHD and a file as the BDT.
    /// </summary>
    public static BXF3 Read(byte[] bhdBytes, string bdtPath)
    {
        using (FileStream bdtStream = File.OpenRead(bdtPath))
        {
            var bhdReader = new BinaryReaderEx(false, bhdBytes);
            var bdtReader = new BinaryReaderEx(false, bdtStream);
            return new BXF3(bhdReader, bdtReader);
        }
    }

    /// <summary>
    ///     Reads a file as the BHD and an array of bytes as the BDT.
    /// </summary>
    public static BXF3 Read(string bhdPath, byte[] bdtBytes)
    {
        using (FileStream bhdStream = File.OpenRead(bhdPath))
        {
            var bhdReader = new BinaryReaderEx(false, bhdStream);
            var bdtReader = new BinaryReaderEx(false, bdtBytes);
            return new BXF3(bhdReader, bdtReader);
        }
    }

    /// <summary>
    ///     Reads a file as the BHD and a file as the BDT.
    /// </summary>
    public static BXF3 Read(string bhdPath, string bdtPath)
    {
        using (FileStream bhdStream = File.OpenRead(bhdPath))
        using (FileStream bdtStream = File.OpenRead(bdtPath))
        {
            var bhdReader = new BinaryReaderEx(false, bhdStream);
            var bdtReader = new BinaryReaderEx(false, bdtStream);
            return new BXF3(bhdReader, bdtReader);
        }
    }

    #endregion

    #region Public Write

    /// <summary>
    ///     Writes the BHD and BDT as two arrays of bytes.
    /// </summary>
    public void Write(out byte[] bhdBytes, out byte[] bdtBytes)
    {
        var bhdWriter = new BinaryWriterEx(false);
        var bdtWriter = new BinaryWriterEx(false);
        Write(bhdWriter, bdtWriter);
        bhdBytes = bhdWriter.FinishBytes();
        bdtBytes = bdtWriter.FinishBytes();
    }

    /// <summary>
    ///     Writes the BHD as an array of bytes and the BDT as a file.
    /// </summary>
    public void Write(out byte[] bhdBytes, string bdtPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(bdtPath));
        using (FileStream bdtStream = File.Create(bdtPath))
        {
            var bhdWriter = new BinaryWriterEx(false);
            var bdtWriter = new BinaryWriterEx(false, bdtStream);
            Write(bhdWriter, bdtWriter);
            bdtWriter.Finish();
            bhdBytes = bhdWriter.FinishBytes();
        }
    }

    /// <summary>
    ///     Writes the BHD as a file and the BDT as an array of bytes.
    /// </summary>
    public void Write(string bhdPath, out byte[] bdtBytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(bhdPath));
        using (FileStream bhdStream = File.Create(bhdPath))
        {
            var bhdWriter = new BinaryWriterEx(false, bhdStream);
            var bdtWriter = new BinaryWriterEx(false);
            Write(bhdWriter, bdtWriter);
            bhdWriter.Finish();
            bdtBytes = bdtWriter.FinishBytes();
        }
    }

    /// <summary>
    ///     Writes the BHD and BDT as two files.
    /// </summary>
    public void Write(string bhdPath, string bdtPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(bhdPath));
        Directory.CreateDirectory(Path.GetDirectoryName(bdtPath));
        using (FileStream bhdStream = File.Create(bhdPath))
        using (FileStream bdtStream = File.Create(bdtPath))
        {
            var bhdWriter = new BinaryWriterEx(false, bhdStream);
            var bdtWriter = new BinaryWriterEx(false, bdtStream);
            Write(bhdWriter, bdtWriter);
            bhdWriter.Finish();
            bdtWriter.Finish();
        }
    }

    #endregion

}