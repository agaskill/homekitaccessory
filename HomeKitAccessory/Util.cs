namespace HomeKitAccessory
{
    using System.IO;
    
    public static class Util
    {
        public static T[] Append<T>(this T[] arr, T value)
        {
            var newarr = new T[arr.Length + 1];
            arr.CopyTo(newarr, 0);
            newarr[arr.Length] = value;
            return newarr;
        }

        public static void Write(this Stream stream, byte[] buff)
        {
            stream.Write(buff, 0, buff.Length);
        }
    }
}