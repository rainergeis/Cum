﻿using System.Collections.Generic;
using System.Linq;

namespace SoulsFormats;

/// <summary>
///     A simple string container used throughout the series.
/// </summary>
public class FMG : SoulsFile<FMG>
{

    /// <summary>
    ///     Indicates the game this FMG is for, and thus the format it will be written in.
    /// </summary>
    public enum FMGVersion : byte
    {
        /// <summary>
        ///     Demon's Souls
        /// </summary>
        DemonsSouls = 0,

        /// <summary>
        ///     Dark Souls 1 and Dark Souls 2
        /// </summary>
        DarkSouls1 = 1,

        /// <summary>
        ///     Bloodborne and Dark Souls 3
        /// </summary>
        DarkSouls3 = 2
    }

    /// <summary>
    ///     FMG file endianness. (Big = true)
    /// </summary>
    public bool BigEndian;

    /// <summary>
    ///     The strings contained in this FMG.
    /// </summary>
    public List<Entry> Entries;

    /// <summary>
    ///     Indicates file format; 0 - DeS, 1 - DS1/DS2, 2 - DS3/BB.
    /// </summary>
    public FMGVersion Version;

    /// <summary>
    ///     Creates an empty FMG configured for DS1/DS2.
    /// </summary>
    public FMG()
    {
        Entries = new List<Entry>();
        Version = FMGVersion.DarkSouls1;
        BigEndian = false;
    }

    /// <summary>
    ///     Creates an empty FMG configured for the specified version.
    /// </summary>
    public FMG(FMGVersion version)
    {
        Entries = new List<Entry>();
        Version = version;
        BigEndian = Version == FMGVersion.DemonsSouls;
    }

    /// <summary>
    ///     Returns the string with the given ID, or null if not present.
    /// </summary>
    public string this[int id]
    {
        get => Entries.Find(entry => entry.ID == id)?.Text;

        set
        {
            if (Entries.Any(entry => entry.ID == id))
                Entries.Find(entry => entry.ID == id).Text = value;
            else
                Entries.Add(new Entry(id, value));
        }
    }

    /// <summary>
    ///     Deserializes file data from a stream.
    /// </summary>
    protected override void Read(BinaryReaderEx br)
    {
        br.AssertByte(0);
        BigEndian = br.ReadBoolean();
        Version = br.ReadEnum8<FMGVersion>();
        br.AssertByte(0);

        br.BigEndian = BigEndian;
        var wide = Version == FMGVersion.DarkSouls3;

        var fileSize = br.ReadInt32();
        br.AssertByte(1);
        br.AssertByte((byte)(Version == FMGVersion.DemonsSouls ? 0xFF : 0x00));
        br.AssertByte(0);
        br.AssertByte(0);
        var groupCount = br.ReadInt32();
        var stringCount = br.ReadInt32();

        if (wide)
            br.AssertInt32(0xFF);

        long stringOffsetsOffset;
        if (wide)
            stringOffsetsOffset = br.ReadInt64();
        else
            stringOffsetsOffset = br.ReadInt32();

        if (wide)
            br.AssertInt64(0);
        else
            br.AssertInt32(0);

        Entries = new List<Entry>(groupCount);
        for (var i = 0; i < groupCount; i++)
        {
            var offsetIndex = br.ReadInt32();
            var firstID = br.ReadInt32();
            var lastID = br.ReadInt32();

            if (wide)
                br.AssertInt32(0);

            br.StepIn(stringOffsetsOffset + offsetIndex * (wide ? 8 : 4));
            {
                for (var j = 0; j < lastID - firstID + 1; j++)
                {
                    long stringOffset;
                    if (wide)
                        stringOffset = br.ReadInt64();
                    else
                        stringOffset = br.ReadInt32();

                    var id = firstID + j;
                    var text = stringOffset != 0 ? br.GetUTF16(stringOffset) : null;
                    Entries.Add(new Entry(id, text));
                }
            }
            br.StepOut();
        }
    }

    /// <summary>
    ///     Serializes file data to a stream.
    /// </summary>
    protected override void Write(BinaryWriterEx bw)
    {
        bw.BigEndian = BigEndian;
        var wide = Version == FMGVersion.DarkSouls3;

        bw.WriteByte(0);
        bw.WriteBoolean(bw.BigEndian);
        bw.WriteByte((byte)Version);
        bw.WriteByte(0);

        bw.ReserveInt32("FileSize");
        bw.WriteByte(1);
        bw.WriteByte((byte)(Version == FMGVersion.DemonsSouls ? 0xFF : 0x00));
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.ReserveInt32("GroupCount");
        bw.WriteInt32(Entries.Count);

        if (wide)
            bw.WriteInt32(0xFF);

        if (wide)
            bw.ReserveInt64("StringOffsets");
        else
            bw.ReserveInt32("StringOffsets");

        if (wide)
            bw.WriteInt64(0);
        else
            bw.WriteInt32(0);

        var groupCount = 0;
        Entries.Sort((e1, e2) => e1.ID.CompareTo(e2.ID));
        for (var i = 0; i < Entries.Count; i++)
        {
            bw.WriteInt32(i);
            bw.WriteInt32(Entries[i].ID);
            while (i < Entries.Count - 1 && Entries[i + 1].ID == Entries[i].ID + 1)
                i++;
            bw.WriteInt32(Entries[i].ID);

            if (wide)
                bw.WriteInt32(0);

            groupCount++;
        }

        bw.FillInt32("GroupCount", groupCount);

        if (wide)
            bw.FillInt64("StringOffsets", bw.Position);
        else
            bw.FillInt32("StringOffsets", (int)bw.Position);

        for (var i = 0; i < Entries.Count; i++)
            if (wide)
                bw.ReserveInt64($"StringOffset{i}");
            else
                bw.ReserveInt32($"StringOffset{i}");

        for (var i = 0; i < Entries.Count; i++)
        {
            var text = Entries[i].Text;

            if (wide)
                bw.FillInt64($"StringOffset{i}", text == null ? 0 : bw.Position);
            else
                bw.FillInt32($"StringOffset{i}", text == null ? 0 : (int)bw.Position);

            if (text != null)
                bw.WriteUTF16(Entries[i].Text, true);
        }

        bw.FillInt32("FileSize", (int)bw.Position);
    }

    /// <summary>
    ///     A string in an FMG identified with an ID number.
    /// </summary>
    public class Entry
    {
        /// <summary>
        ///     The ID of this entry.
        /// </summary>
        public int ID;

        /// <summary>
        ///     The text of this entry.
        /// </summary>
        public string Text;

        /// <summary>
        ///     Creates a new entry with the specified ID and text.
        /// </summary>
        public Entry(int id, string text)
        {
            ID = id;
            Text = text;
        }

        /// <summary>
        ///     Returns the ID and text of this entry.
        /// </summary>
        public override string ToString()
        {
            return $"{ID}: {Text ?? "<null>"}";
        }
    }
}