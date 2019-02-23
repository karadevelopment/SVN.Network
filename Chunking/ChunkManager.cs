using System.Collections.Generic;
using System.Linq;

namespace SVN.Network.Chunking
{
    public static class ChunkManager
    {
        private static List<ChunkFile> Files { get; } = new List<ChunkFile>();

        public static bool HasFile(int id)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Id == id);
            return (file != null);
        }

        public static bool HasChunk(int id, int index)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Id == id);
            return (file != null && file.HasChunk(index));
        }

        public static byte[] GetChunk(int id, int index)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Id == id);

            if (file != null)
            {
                return file.GetChunk(index);
            }

            return new byte[0];
        }

        public static void AddChunk(int id, int index, byte[] bytes)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Id == id);

            if (file == null)
            {
                ChunkManager.Files.Add(file = new ChunkFile(id));
            }

            file.AddChunk(index, bytes);
        }

        public static void AddFile(int id, byte[] bytes)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Id == id);

            if (file == null)
            {
                ChunkManager.Files.Add(new ChunkFile(id, bytes));
            }
        }

        public static bool IsBuildable(int id)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Id == id);
            return (file != null && file.IsBuildable());
        }

        public static byte[] Build(int id)
        {
            var file = ChunkManager.Files.FirstOrDefault(x => x.Id == id);

            if (file != null && file.IsBuildable())
            {
                ChunkManager.Files.Remove(file);
                return file.Build();
            }

            return new byte[0];
        }
    }
}