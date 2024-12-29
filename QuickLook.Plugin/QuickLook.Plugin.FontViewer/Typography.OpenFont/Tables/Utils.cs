//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev 

using System;
using System.Text;
using System.IO;

namespace Typography.OpenFont
{
    static class Utils
    {
        /// <summary>
        /// read float, 2.14 format
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static float ReadF2Dot14(this BinaryReader reader)
        {
            return ((float)reader.ReadInt16()) / (1 << 14); /* Format 2.14 */
        }

        public static Bounds ReadBounds(BinaryReader input) 
        {
            return new Bounds(
                input.ReadInt16(),//xmin
                input.ReadInt16(), //ymin
                input.ReadInt16(), //xmax
                input.ReadInt16());//ymax
        }

        public static string TagToString(uint tag)
        {
            byte[] bytes = BitConverter.GetBytes(tag);
            Array.Reverse(bytes);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        public static int ReadUInt24(this BinaryReader reader)
        {
            byte highByte = reader.ReadByte();
            return (highByte << 16) | reader.ReadUInt16();
        }
        /// <summary>
        /// 16.16 float format
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static float ReadFixed(this BinaryReader reader)
        {
            //16.16 format
            return (float)reader.ReadUInt32() / (1 << 16);
        }
        
        public static ushort[] ReadUInt16Array(this BinaryReader reader, int nRecords)
        {
            ushort[] arr = new ushort[nRecords];
            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = reader.ReadUInt16();
            }
            return arr;
        }
        public static uint[] ReadUInt16ArrayAsUInt32Array(this BinaryReader reader, int nRecords)
        {
            uint[] arr = new uint[nRecords];
            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = reader.ReadUInt16();
            }
            return arr;
        }
        public static uint[] ReadUInt32Array(this BinaryReader reader, int nRecords)
        {
            uint[] arr = new uint[nRecords];
            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = reader.ReadUInt32();
            }
            return arr;
        }

        public static T[] CloneArray<T>(T[] original, int newArrLenExtend = 0)
        {
            int orgLen = original.Length;
            T[] newClone = new T[orgLen + newArrLenExtend];
            Array.Copy(original, newClone, orgLen);
            return newClone;
        }

        public static T[] ConcatArray<T>(T[] arr1, T[] arr2)
        {
            T[] newArr = new T[arr1.Length + arr2.Length];
            Array.Copy(arr1, 0, newArr, 0, arr1.Length);
            Array.Copy(arr2, 0, newArr, arr1.Length, arr2.Length);
            return newArr;
        }

        public static void WarnUnimplemented(string format, params object[] args)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("!STUB! " + string.Format(format, args));
#endif
        }

        internal static void WarnUnimplementedCollectAssocGlyphs(string msg)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("!STUB! UnimplementedCollectAssocGlyph :" + msg);
#endif
        }
#if DEBUG
        public static bool dbugIsDiff(GlyphPointF[] set1, GlyphPointF[] set2)
        {
            int j = set1.Length;
            if (j != set2.Length)
            {
                //yes, diff
                return true;
            }
            for (int i = j - 1; i >= 0; --i)
            {
                if (!set1[i].dbugIsEqualsWith(set2[i]))
                {
                    //yes, diff
                    return true;
                }
            }
            //no, both are the same
            return false;
        }
#endif
    }
}
