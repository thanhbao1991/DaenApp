using System.Runtime.InteropServices;

namespace TraSuaApp.Shared.Helpers
{
    public class RawPrinterHelper
    {
        // Structure and API declarations:
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA",
            SetLastError = true, CharSet = CharSet.Ansi,
            ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter",
            SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA",
            SetLastError = true, CharSet = CharSet.Ansi,
            ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level,
            [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter",
            SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter",
            SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter",
            SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter",
            SetLastError = true, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        /// <summary>
        /// Gửi chuỗi ra máy in
        /// </summary>
        public static bool SendStringToPrinter(string printerName, string data)
        {
            IntPtr pBytes;
            int dwCount = data.Length;
            pBytes = Marshal.StringToCoTaskMemAnsi(data);
            try
            {
                return SendBytesToPrinter(printerName, pBytes, dwCount);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pBytes);
            }
        }

        /// <summary>
        /// Gửi mảng byte ra máy in
        /// </summary>
        public static bool SendBytesToPrinter(string printerName, IntPtr pBytes, int dwCount)
        {
            IntPtr hPrinter;
            DOCINFOA di = new DOCINFOA
            {
                pDocName = "InHoaDon",
                pDataType = "RAW"
            };

            bool success = false;
            if (OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        int dwWritten;
                        success = WritePrinter(hPrinter, pBytes, dwCount, out dwWritten);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                throw new IOException($"Lỗi in: {error}");
            }

            return success;
        }
    }
}