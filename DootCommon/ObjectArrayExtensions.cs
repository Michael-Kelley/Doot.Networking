using System.Diagnostics;



namespace Doot
{
    public static class ObjectArrayExtensions
    {
        public static (T1, T2) ToValueTuple<T1, T2>(this object[] array)
        {
            Debug.Assert(array != null);
            Debug.Assert(array.Length == 2);
            Debug.Assert(array[0].GetType() == typeof(T1));
            Debug.Assert(array[1].GetType() == typeof(T2));

            return ((T1)array[0], (T2)array[1]);
        }

        public static (T1, T2, T3) ToValueTuple<T1, T2, T3>(this object[] array)
        {
            Debug.Assert(array != null);
            Debug.Assert(array.Length == 3);
            Debug.Assert(array[0].GetType() == typeof(T1));
            Debug.Assert(array[1].GetType() == typeof(T2));
            Debug.Assert(array[2].GetType() == typeof(T3));

            return ((T1)array[0], (T2)array[1], (T3)array[2]);
        }

        public static (T1, T2, T3, T4) ToValueTuple<T1, T2, T3, T4>(this object[] array)
        {
            Debug.Assert(array != null);
            Debug.Assert(array.Length == 4);
            Debug.Assert(array[0].GetType() == typeof(T1));
            Debug.Assert(array[1].GetType() == typeof(T2));
            Debug.Assert(array[2].GetType() == typeof(T3));
            Debug.Assert(array[3].GetType() == typeof(T4));

            return ((T1)array[0], (T2)array[1], (T3)array[2], (T4)array[3]);
        }
    }
}
