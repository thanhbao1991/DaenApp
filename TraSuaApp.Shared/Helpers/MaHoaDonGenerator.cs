namespace TraSuaApp.Shared.Helpers
{
    public static class MaHoaDonGenerator
    {
        private static readonly char[] Base36Chars =
    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        public static string ToBase36(long value)
        {
            if (value < 0) throw new ArgumentException("Giá trị phải >= 0", nameof(value));

            char[] buffer = new char[13]; // tối đa 13 ký tự cho long
            int index = buffer.Length;

            do
            {
                buffer[--index] = Base36Chars[value % 36];
                value /= 36;
            }
            while (value > 0);

            return new string(buffer, index, buffer.Length - index);
        }
        public static string Generate()
        {
            long ticks = DateTime.UtcNow.Ticks;
            return ToBase36(ticks);
        }
    }
}