using System;
using System.Runtime.CompilerServices;

namespace FLCCustom3DPath {
    public static class ArrayExt {
        /// <summary>
        /// checks to see if value exists in source
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="value">Value.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this T[] source, T value) {
            return Array.IndexOf(source, value) >= 0;
        }

        /// <summary>
        /// Adds a new element to the array and returns the array,
        /// If the element doesn't fit in the array, it is reallocated
        /// on a new array with twice as much space.
        /// </summary>
        public static T[] Add<T>(this T[] source, ref int count, T value) {
            if(count >= source.Length ) {
                T[] newArray = new T[source.Length*2];
                Array.Copy(source, newArray, source.Length);
                newArray[count] = value;
                ++count;
                return newArray;
            }
            else {
                source[count] = value;
                ++count;
                return source;
            }
        }

        /// <summary>
        /// Adds a new element to the array and returns the array,
        /// If the element doesn't fit in the array, it is reallocated
        /// on a new array with the length of the old array + a custom amount
        /// </summary>
        public static T[] Add<T>(this T[] source, ref int count, T value, uint expandAmount) {
            if(count >= source.Length ) {
                T[] newArray = new T[source.Length + expandAmount];
                Array.Copy(source, newArray, source.Length);
                newArray[count] = value;
                ++count;
                return newArray;
            }
            else {
                source[count] = value;
                ++count;
                return source;
            }
        }

        public static T[] Trim<T>(this T[] source, ref int count) {
            if(source.Length > count) {
                var newArray = new T[count];
                Array.Copy(source,newArray,count);
                return newArray;
            }
            return source;
        }

        /// <summary>
        /// Removes the element at index T and fills the gap with the last element of
        /// the array
        /// </summary>
        /// <param name="source"></param>
        /// <param name="count"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        public static void RemoveAtIndex<T>(this T[] source, ref int count, int index) {
            if (count == 0 | index >= count)return;
            source[index] = source[count-1];
            source[count-1] = default(T);
            --count;
        }

        public static T[] Clear<T>(this T[] source, ref int count, bool trim) {
            if(trim && count < source.Length) {
                var newArray = new T[count];
                count = 0;
                return newArray;
            }
            Array.Clear(source,0,count);
            count = 0;
            return source;
        }
    }
}