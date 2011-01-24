using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace QRBuild.Translations
{
    /// A simple Win32 process class that avoids basic
    /// problems, like handle leakage.
    public sealed class QRProcess : IDisposable
    {
        private QRProcess(PROCESS_INFORMATION processInfo)
        {
            m_processInfo = processInfo;
            m_processWaitHandle = new ProcessWaitHandle(m_processInfo.hProcess);
        }

        public WaitHandle WaitHandle 
        { 
            get { return m_processWaitHandle; } 
        }

        public int Id
        {
            get { return m_processInfo.dwProcessId; }
        }

        public uint GetExitCode()
        {
            uint exitCode;
            GetExitCodeProcess(m_processInfo.hProcess, out exitCode);
            return exitCode;
        }

        public void Dispose()
        {
            m_processWaitHandle.Close();
            CloseHandle(m_processInfo.hProcess);
            CloseHandle(m_processInfo.hThread);
        }


        private sealed class ProcessWaitHandle : WaitHandle
        {
            public ProcessWaitHandle(IntPtr hProcess)
            {
                SafeWaitHandle = new SafeWaitHandle(hProcess, false);
            }
        }

        private PROCESS_INFORMATION m_processInfo;
        private WaitHandle m_processWaitHandle;

        const uint NORMAL_PRIORITY_CLASS = 0x00000020;

        const uint CREATE_NEW_CONSOLE = 0x00000010;
        const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        const uint CREATE_NO_WINDOW = 0x08000000;
        const int STARTF_USESHOWWINDOW = 0x00000001;
        const int STARTF_USESTDHANDLES = 0x00000100;
        const short SW_HIDE = 0;
        public const uint STILL_ACTIVE = 259;
    
        /// Launch a batch file.
        /// This function calls CreateProcess directly, and ensures that 
        /// bInheritHandles=false, which prevents handle leakage into 
        /// child processes.
        public static QRProcess LaunchBatchFile(
            string batchFilePath, 
            string workingDir,
            bool unicode, 
            string extraCmdCommandLine)
        {
            string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string cmdPath = Path.Combine(systemPath, "cmd.exe");

            StringBuilder commandline = new StringBuilder();
            if (unicode) {
                commandline.Append("/U ");
            }
            commandline.Append("/C "); // terminate after executing
            commandline.AppendFormat("\"{0} {1}\" ", 
                batchFilePath,
                String.IsNullOrEmpty(extraCmdCommandLine) ? "" : extraCmdCommandLine);

            uint dwCreationFlags = CREATE_NEW_CONSOLE | NORMAL_PRIORITY_CLASS;

            STARTUPINFO startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.dwFlags = STARTF_USESHOWWINDOW;
            startupInfo.wShowWindow = SW_HIDE;

            PROCESS_INFORMATION processInfo;

            bool success = CreateProcess(
                cmdPath, 
                commandline, 
                null, 
                null, 
                /* bInheritHandles */ false,
                dwCreationFlags, 
                IntPtr.Zero, 
                workingDir, 
                ref startupInfo, 
                out processInfo);
            if (!success) {
                int gle = Marshal.GetLastWin32Error();
                return null;
            }

            QRProcess process = new QRProcess(processInfo);
            return process;
        }        
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }
        
        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateProcess(
            string lpApplicationName,
            StringBuilder lpCommandLine, 
            SECURITY_ATTRIBUTES lpProcessAttributes, 
            SECURITY_ATTRIBUTES lpThreadAttributes, 
            bool bInheritHandles, 
            uint dwCreationFlags, 
            IntPtr lpEnvironment, 
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo, 
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);    
    }
}
