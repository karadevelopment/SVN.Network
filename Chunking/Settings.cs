using System;

namespace SVN.Network.Chunking
{
    internal static class Settings
    {
        public static TimeSpan Lifetime { get; } = TimeSpan.FromMinutes(5);
        public static long ChunkSize { get; } = (long)Math.Pow(2, 25);
    }
}