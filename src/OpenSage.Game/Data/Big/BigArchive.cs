﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OpenSage.Data.Utilities.Extensions;

namespace OpenSage.Data.Big
{
    public class BigArchive : IDisposable
    {
        private readonly object _lockObject = new object();

        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly BinaryReader _reader;

        private readonly List<BigArchiveEntry> _entries;
        private readonly Dictionary<string, BigArchiveEntry> _entriesDictionary;

        public IReadOnlyList<BigArchiveEntry> Entries => _entries;

        internal Stream Stream => _stream;

        public BigArchiveVersion Version { get; private set; }

        public BigArchive(Stream stream, bool leaveOpen = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _leaveOpen = leaveOpen;

            if (!_stream.CanRead)
            {
                throw new ArgumentException("Stream is not readable.");
            }

            if (!_stream.CanSeek)
            {
                throw new ArgumentException("Stream is not seekable.");
            }

            _stream.Seek(0, SeekOrigin.Begin);

            _reader = new BinaryReader(_stream);

            _entries = new List<BigArchiveEntry>();
            _entriesDictionary = new Dictionary<string, BigArchiveEntry>();

            Read();
        }

        internal void AcquireLock()
        {
            Monitor.Enter(_lockObject);
        }

        internal void ReleaseLock()
        {
            Monitor.Exit(_lockObject);
        }

        private void Read()
        {
            var fourCc = _reader.ReadUInt32().ToFourCcString();
            switch (fourCc)
            {
                case "BIGF":
                    Version = BigArchiveVersion.BigF;
                    break;

                case "BIG4":
                    Version = BigArchiveVersion.Big4;
                    break;

                default:
                    throw new InvalidDataException($"Not a supported BIG format: {fourCc}");
            }

            _reader.ReadBigEndianUInt32(); // Archive Size
            var numEntries = _reader.ReadBigEndianUInt32();
            _reader.ReadBigEndianUInt32(); // First File Offset

            for (var i = 0; i < numEntries; i++)
            {
                var entryOffset = _reader.ReadBigEndianUInt32();
                var entrySize = _reader.ReadBigEndianUInt32();
                var entryName = _reader.ReadNullTerminatedString();

                var entry = new BigArchiveEntry(this, entryName, entryOffset, entrySize);

                _entries.Add(entry);
                _entriesDictionary.Add(entryName, entry);
            }
        }

        public BigArchiveEntry GetEntry(string entryName)
        {
            if (entryName == null)
            {
                throw new ArgumentNullException(nameof(entryName));
            }

            _entriesDictionary.TryGetValue(entryName, out var result);
            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_leaveOpen)
            {
                _stream.Dispose();
                _reader.Dispose();
            }
        }
    }

    public enum BigArchiveVersion
    {
        BigF,
        Big4
    }
}
