using SVN.Core.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SVN.Network.Chunking
{
    internal class ChunkFile
    {
        public string Identifier { get; private set; }
        private int Length { get; set; }
        private List<Chunk> Chunks { get; } = new List<Chunk>();
        private DateTime LastActivity { get; set; } = DateTime.Now;

        public ChunkFile(string identifier, int length = default(int))
        {
            this.Identifier = identifier;
            this.Length = length;
        }

        public ChunkFile(string identifier, byte[] bytes)
        {
            this.Identifier = identifier;
            this.Length = bytes.Length;

            for (var i = 1; i <= Math.Ceiling((double)this.Length / Settings.ChunkSize); i++)
            {
                this.AddChunk(i - 1, bytes.Skip((int)((i - 1) * Settings.ChunkSize)).Take((int)Settings.ChunkSize).ToArray());
            }
        }

        public bool IsActive
        {
            get => DateTime.Now - this.LastActivity < Settings.Lifetime;
        }

        public bool HasChunk(int index)
        {
            lock (this.Chunks)
            {
                return this.Chunks.Any(x => x.Index == index);
            }
        }

        public byte[] GetChunk(int index)
        {
            lock (this.Chunks)
            {
                this.LastActivity = DateTime.Now;
                return this.Chunks.Where(x => x.Index == index).Select(x => x.Bytes).FirstOrDefault();
            }
        }

        public void AddChunk(int index, byte[] bytes)
        {
            lock (this.Chunks)
            {
                this.LastActivity = DateTime.Now;
                this.Chunks.Add(new Chunk(index, bytes));
            }
        }

        public bool IsBuildable()
        {
            var length = default(int) < this.Length ? this.Length : this.Chunks.Max(x => x.Index) + 1;

            for (var i = 1; i <= length; i++)
            {
                lock (this.Chunks)
                {
                    if (!this.Chunks.Any(x => x.Index == i - 1))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public byte[] Build()
        {
            lock (this.Chunks)
            {
                return this.Chunks.DistinctBy(x => x.Index).OrderBy(x => x.Index).SelectMany(x => x.Bytes).ToArray();
            }
        }
    }
}