
namespace Doot.Tests
{
    public static class Assert
    {
        public static void True(bool condition)
        {
            if (!condition)
                throw new AssertionException("Assert.True failed!");
        }

        public static void False(bool condition)
        {
            if (condition)
                throw new AssertionException("Assert.False failed!");
        }

        public static void Equal<T>(T a, T b)
        {
            if (!EqualityComparer<T>.Default.Equals(a, b))
                throw new AssertionException("Assert.Equal failed!");
        }

        public static void NotEqual<T>(T a, T b)
        {
            if (EqualityComparer<T>.Default.Equals(a, b))
                throw new AssertionException("Assert.NotEqual failed!");
        }

        public static void Greater<T>(T a, T b)
        {
            if (Comparer<T>.Default.Compare(a, b) <= 0)
                throw new AssertionException("Assert.Greater failed!");
        }

        public static void Less<T>(T a, T b)
        {
            if (Comparer<T>.Default.Compare(a, b) >= 0)
                throw new AssertionException("Assert.Less failed!");
        }

        public static void GreaterOrEqual<T>(T a, T b)
        {
            if (Comparer<T>.Default.Compare(a, b) < 0)
                throw new AssertionException("Assert.GreaterOrEqual failed!");
        }

        public static void LessOrEqual<T>(T a, T b)
        {
            if (Comparer<T>.Default.Compare(a, b) > 0)
                throw new AssertionException("Assert.LessOrEqual failed!");
        }
    }
}
