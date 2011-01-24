using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QRBuild.IO
{
    public static class QRSystem
    {
        public static readonly bool Is64BitProcess =
            IntPtr.Size == 8;
        public static readonly bool Is64BitOperatingSystem = 
            Is64BitProcess || IsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        /// Code from http://stackoverflow.com/questions/336633/how-to-detect-windows-64-bit-platform-with-net
        public static bool IsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6) {
                using (Process p = Process.GetCurrentProcess()) {
                    bool isWow64Process;
                    if (!IsWow64Process(p.Handle, out isWow64Process)) {
                        return false;
                    }
                    return isWow64Process;
                }
            }
            else {
                return false;
            }
        }

    }
}
