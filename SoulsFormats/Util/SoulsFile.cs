﻿using System;
using System.IO;

namespace SoulsFormats;

/// <summary>
///     A generic From file supporting transparent DCX reading and writing.
/// </summary>
public abstract class SoulsFile<TFormat> : ISoulsFile where TFormat : SoulsFile<TFormat>, new()
{
    /// <summary>
    ///     The type of DCX compression to be used when writing.
    /// </summary>
    public DCX.Type Compression { get; set; } = DCX.Type.None;

    /// <summary>
    ///     Writes the file to an array of bytes, automatically compressing it if necessary.
    /// </summary>
    public byte[] Write()
    {
        return Write(Compression);
    }

    /// <summary>
    ///     Writes the file to an array of bytes, compressing it as specified.
    /// </summary>
    public byte[] Write(DCX.Type compression)
    {
        if (!Validate(out Exception ex))
            throw ex;

        var bw = new BinaryWriterEx(false);
        Write(bw, compression);
        return bw.FinishBytes();
    }

    /// <summary>
    ///     Writes the file to the specified path, automatically compressing it if necessary.
    /// </summary>
    public void Write(string path)
    {
        Write(path, Compression);
    }

    /// <summary>
    ///     Writes the file to the specified path, compressing it as specified.
    /// </summary>
    public void Write(string path, DCX.Type compression)
    {
        if (!Validate(out Exception ex))
            throw ex;

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (FileStream stream = File.Create(path))
        {
            var bw = new BinaryWriterEx(false, stream);
            Write(bw, compression);
            bw.Finish();
        }
    }

    /// <summary>
    ///     Returns true if the data appears to be a file of this type.
    /// </summary>
    // This should really be a static method, but interfaces do not allow static inheritance; hence the dummy objects below.
    protected virtual bool Is(BinaryReaderEx br)
    {
        throw new NotImplementedException("Is is not implemented for this format.");
    }

    /// <summary>
    ///     Returns true if the bytes appear to be a file of this type.
    /// </summary>
    public static bool Is(byte[] bytes)
    {
        if (bytes.Length == 0)
            return false;

        var br = new BinaryReaderEx(false, bytes);
        var dummy = new TFormat();
        return dummy.Is(SFUtil.GetDecompressedBR(br, out _));
    }

    /// <summary>
    ///     Returns true if the file appears to be a file of this type.
    /// </summary>
    public static bool Is(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        {
            if (stream.Length == 0)
                return false;

            var br = new BinaryReaderEx(false, stream);
            var dummy = new TFormat();
            return dummy.Is(SFUtil.GetDecompressedBR(br, out _));
        }
    }

    /// <summary>
    ///     Loads file data from a BinaryReaderEx.
    /// </summary>
    protected virtual void Read(BinaryReaderEx br)
    {
        throw new NotImplementedException("Read is not implemented for this format.");
    }

    /// <summary>
    ///     Loads a file from a byte array, automatically decompressing it if necessary.
    /// </summary>
    public static TFormat Read(byte[] bytes)
    {
        var br = new BinaryReaderEx(false, bytes);
        var file = new TFormat();
        br = SFUtil.GetDecompressedBR(br, out DCX.Type compression);
        file.Compression = compression;
        file.Read(br);
        return file;
    }

    /// <summary>
    ///     Loads a file from the specified path, automatically decompressing it if necessary.
    /// </summary>
    public static TFormat Read(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        {
            var br = new BinaryReaderEx(false, stream);
            var file = new TFormat();
            br = SFUtil.GetDecompressedBR(br, out DCX.Type compression);
            file.Compression = compression;
            file.Read(br);
            return file;
        }
    }

    private static bool IsRead(BinaryReaderEx br, out TFormat file)
    {
        var test = new TFormat();
        br = SFUtil.GetDecompressedBR(br, out DCX.Type compression);
        if (test.Is(br))
        {
            br.Position = 0;
            test.Compression = compression;
            test.Read(br);
            file = test;
            return true;
        }

        file = null;
        return false;
    }

    /// <summary>
    ///     Returns whether the bytes appear to be a file of this type and reads it if so.
    /// </summary>
    public static bool IsRead(byte[] bytes, out TFormat file)
    {
        var br = new BinaryReaderEx(false, bytes);
        return IsRead(br, out file);
    }

    /// <summary>
    ///     Returns whether the file appears to be a file of this type and reads it if so.
    /// </summary>
    public static bool IsRead(string path, out TFormat file)
    {
        using (FileStream fs = File.OpenRead(path))
        {
            var br = new BinaryReaderEx(false, fs);
            return IsRead(br, out file);
        }
    }

    /// <summary>
    ///     Checks the object for any fatal problems; Write will throw the returned exception on failure.
    /// </summary>
    public virtual bool Validate(out Exception ex)
    {
        ex = null;
        return true;
    }

    /// <summary>
    ///     Returns whether the object is not null, otherwise setting ex to a NullReferenceException with the given message.
    /// </summary>
    protected static bool ValidateNull(object obj, string message, out Exception ex)
    {
        if (obj == null)
        {
            ex = new NullReferenceException(message);
            return false;
        }

        ex = null;
        return true;
    }

    /// <summary>
    ///     Returns whether the index is in range, otherwise setting ex to an IndexOutOfRangeException with the given message.
    /// </summary>
    protected static bool ValidateIndex(long count, long index, string message, out Exception ex)
    {
        if (index < 0 || index >= count)
        {
            ex = new IndexOutOfRangeException(message);
            return false;
        }

        ex = null;
        return true;
    }

    /// <summary>
    ///     Writes file data to a BinaryWriterEx.
    /// </summary>
    protected virtual void Write(BinaryWriterEx bw)
    {
        throw new NotImplementedException("Write is not implemented for this format.");
    }

    /// <summary>
    ///     Writes file data to a BinaryWriterEx, compressing it afterwards if specified.
    /// </summary>
    private void Write(BinaryWriterEx bw, DCX.Type compression)
    {
        if (compression == DCX.Type.None)
        {
            Write(bw);
        }
        else
        {
            var bwUncompressed = new BinaryWriterEx(false);
            Write(bwUncompressed);
            var uncompressed = bwUncompressed.FinishBytes();
            DCX.Compress(uncompressed, bw, compression);
        }
    }
}