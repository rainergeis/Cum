﻿using System.Collections.Generic;

namespace SoulsFormats;

public partial class DRB
{
    /// <summary>
    ///     Unknown.
    /// </summary>
    public class Scdl
    {

        /// <summary>
        ///     Creates a Scdl with default values.
        /// </summary>
        public Scdl()
        {
            Name = "";
            Scdos = new List<Scdo>();
        }

        internal Scdl(BinaryReaderEx br, Dictionary<int, string> strings, Dictionary<int, Scdo> scdos)
        {
            var nameOffset = br.ReadInt32();
            var scdoCount = br.ReadInt32();
            var scdoOffset = br.ReadInt32();
            Unk0C = br.ReadInt32();

            Name = strings[nameOffset];
            Scdos = new List<Scdo>(scdoCount);
            for (var i = 0; i < scdoCount; i++)
            {
                var offset = scdoOffset + SCDO_SIZE * i;
                Scdos.Add(scdos[offset]);
                scdos.Remove(offset);
            }
        }

        /// <summary>
        ///     The name of this Scdl.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Scdos in this Scdl.
        /// </summary>
        public List<Scdo> Scdos { get; set; }

        /// <summary>
        ///     Unknown.
        /// </summary>
        public int Unk0C { get; set; }

        internal void Write(BinaryWriterEx bw, Dictionary<string, int> stringOffsets, Queue<int> scdoOffsets)
        {
            bw.WriteInt32(stringOffsets[Name]);
            bw.WriteInt32(Scdos.Count);
            bw.WriteInt32(scdoOffsets.Dequeue());
            bw.WriteInt32(Unk0C);
        }

        /// <summary>
        ///     Returns the name and number of Scdos.
        /// </summary>
        public override string ToString()
        {
            return $"{Name}[{Scdos.Count}]";
        }
    }
}