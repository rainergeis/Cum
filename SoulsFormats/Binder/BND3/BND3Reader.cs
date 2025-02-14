﻿using System.IO;

namespace SoulsFormats;

/// <summary>
///     An on-demand reader for BND3 containers.
/// </summary>
public class BND3Reader : BinderReader, IBND3
{

    /// <summary>
    ///     Reads a BND3 from the given path, decompressing if necessary.
    /// </summary>
    public BND3Reader(string path)
    {
        FileStream fs = File.OpenRead(path);
        var br = new BinaryReaderEx(false, fs);
        Read(br);
    }

    /// <summary>
    ///     Reads a BND3 from the given bytes, decompressing if necessary.
    /// </summary>
    public BND3Reader(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        var br = new BinaryReaderEx(false, ms);
        Read(br);
    }

    /// <summary>
    ///     Type of compression used, if any.
    /// </summary>
    public DCX.Type Compression { get; set; }

    /// <summary>
    ///     Unknown; always 0 except in DeS where it's occasionally 0x80000000 (probably a byte).
    /// </summary>
    public int Unk18 { get; set; }

    private void Read(BinaryReaderEx br)
    {
        br = SFUtil.GetDecompressedBR(br, out DCX.Type compression);
        Compression = compression;
        Files = BND3.ReadHeader(this, br);
        DataBR = br;
    }
}