
namespace Doot
{
    public static class ObjectArrayExtensions
    {
        public static (T1, T2) ToValueTuple<T1, T2>(this object[] array)
        {
            return ((T1)array[0], (T2)array[1]);
        }

        public static (T1, T2, T3) ToValueTuple<T1, T2, T3>(this object[] array)
        {
            return ((T1)array[0], (T2)array[1], (T3)array[2]);
        }

        public static (T1, T2, T3, T4) ToValueTuple<T1, T2, T3, T4>(this object[] array)
        {
            return ((T1)array[0], (T2)array[1], (T3)array[2], (T4)array[3]);
        }
    }
}
