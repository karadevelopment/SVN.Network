namespace SVN.Network.Chunking
{
    internal class Chunk
    {
        public int Index { get; private set; }
        public byte[] Bytes { get; private set; }

        public Chunk(int index, byte[] bytes)
        {
            this.Index = index;
            this.Bytes = bytes;
        }
    }
}