﻿using System.IO;
using OpenZH.Data.Utilities.Extensions;

namespace OpenZH.Data.Wak
{
    public sealed class WakEntry
    {
        public float StartX { get; private set; }
        public float StartY { get; private set; }
        public float EndX { get; private set; }
        public float EndY { get; private set; }
        public WakEntryType EntryType { get; private set; }

        public static WakEntry Parse(BinaryReader reader)
        {
            return new WakEntry
            {
                StartX = reader.ReadSingle(),
                StartY = reader.ReadSingle(),
                EndX = reader.ReadSingle(),
                EndY = reader.ReadSingle(),

                EntryType = reader.ReadUInt32AsEnum<WakEntryType>()
            };
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(StartX);
            writer.Write(StartY);
            writer.Write(EndX);
            writer.Write(EndY);

            writer.Write((uint) EntryType);
        }
    }
}