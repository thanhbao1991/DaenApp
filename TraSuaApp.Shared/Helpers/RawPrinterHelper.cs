using System.Runtime.InteropServices;
using System.Text;

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
        public static void ProbeCodePages(string printerName)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc = Encoding.GetEncoding("windows-1258");

            string sample = "Test VI: Điểm tháng này – ă â ê ô ơ ư đ";

            for (int n = 0; n <= 39; n++)
            {
                var header = new byte[] { 0x1B, 0x40, 0x1B, 0x74, (byte)n };
                var body = enc.GetBytes($"ESC t {n:D2} => {sample}\r\n");
                var all = new byte[header.Length + body.Length];
                Buffer.BlockCopy(header, 0, all, 0, header.Length);
                Buffer.BlockCopy(body, 0, all, header.Length, body.Length);

                IntPtr unmanaged = Marshal.AllocCoTaskMem(all.Length);
                try
                {
                    Marshal.Copy(all, 0, unmanaged, all.Length);
                    SendBytesToPrinter(printerName, unmanaged, all.Length);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(unmanaged);
                }
            }
        }
        /// <summary>
        /// Gửi chuỗi ra máy in
        /// </summary>
        public static bool SendStringToPrinter(
                   string printerName,
                   string data,
                   string codepage = "windows-1258",
                   int escposCodePageIndex = 30 // đa số Xprinter: 30 = Vietnamese
               )
        {
            // Cho phép lấy các codepage ngoài UTF-8 trong .NET
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var enc = Encoding.GetEncoding(codepage);

            // Dải lệnh ESC/POS: reset & chọn bảng mã trước khi in
            // ESC @  -> reset
            // ESC t n -> chọn bảng mã (n = 30 thường là Vietnamese trên Xprinter)
            var header = new byte[] { 0x1B, 0x40, 0x1B, 0x74, (byte)escposCodePageIndex };

            // Nội dung hóa đơn
            byte[] body = enc.GetBytes(data);

            // Ghép và gửi
            byte[] all = new byte[header.Length + body.Length];
            Buffer.BlockCopy(header, 0, all, 0, header.Length);
            Buffer.BlockCopy(body, 0, all, header.Length, body.Length);

            IntPtr unmanaged = Marshal.AllocCoTaskMem(all.Length);
            try
            {
                Marshal.Copy(all, 0, unmanaged, all.Length);
                return SendBytesToPrinter(printerName, unmanaged, all.Length);
            }
            finally
            {
                Marshal.FreeCoTaskMem(unmanaged);
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