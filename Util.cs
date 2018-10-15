namespace HomeKitAccessory
{
    public static class Util
    {
        public static T[] Append<T>(this T[] arr, T value)
        {
            var newarr = new T[arr.Length + 1];
            arr.CopyTo(newarr, 0);
            newarr[arr.Length] = value;
            return newarr;
        }
    }
}