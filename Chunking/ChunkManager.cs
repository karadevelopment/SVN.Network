using System.Collections.Generic;
using System.Linq;

namespace SVN.Network.Chunking
{
    public static class ChunkManager
    {
        private static List<ChunkFile> Files { get; } = new List<ChunkFile>();

        public static bool HasFile(string identifier)
        {
            return ChunkManager.Files.Any(x => x.Identifier == identifier);
        }

        public static bool HasChunk(string identifier, int index)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Identifier == identifier);
            return file != null && file.HasChunk(index);
        }

        public static byte[] GetChunk(string identifier, int index)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Identifier == identifier);

            if (file != null)
            {
                return file.GetChunk(index);
            }

            return new byte[0];
        }

        public static void AddFile(string identifier)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Identifier == identifier);

            if (file is null)
            {
                ChunkManager.Files.Add(new ChunkFile(identifier));
            }
        }

        public static void AddFile(string identifier, byte[] bytes)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Identifier == identifier);

            if (file is null)
            {
                ChunkManager.Files.Add(new ChunkFile(identifier, bytes));
            }
        }

        public static void AddChunk(string identifier, int index, byte[] bytes)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Identifier == identifier);

            if (file is null)
            {
                ChunkManager.Files.Add(file = new ChunkFile(identifier));
            }

            file.AddChunk(index, bytes);
        }

        public static bool IsBuildable(string identifier)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Identifier == identifier);
            return file != null && file.IsBuildable();
        }

        public static byte[] Build(string identifier)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Identifier == identifier);

            if (file != null && file.IsBuildable())
            {
                ChunkManager.Files.Remove(file);
                return file.Build();
            }

            return new byte[0];
        }
    }
}