using System.Text;



namespace Doot
{
    static class FNV1a
    {
        const ulong FNV64_OFFSET = 14695981039346656037;
        const ulong FNV64_PRIME = 0x100000001b3;

        public static ulong ComputeHash(byte[] bytes)
        {
            ulong hash = FNV64_OFFSET;

            for (var i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= FNV64_PRIME;
            }

            return hash;
        }

        public static ulong ComputeHash(string value)
        {
            return ComputeHash(Encoding.UTF8.GetBytes(value));
        }
    }
}
