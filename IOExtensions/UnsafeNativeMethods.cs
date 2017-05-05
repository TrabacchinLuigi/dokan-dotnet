using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SilentWave.IOExtensons
{
    internal static class UnsafeNativeMethods
    {
        [DllImport("Kernel32", SetLastError = true)]
        [System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.None)]
        public unsafe static extern bool SetFileTime(
           Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
           FILE_TIME* creationTime,
           FILE_TIME* lastAccessTime,
           FILE_TIME* lastWriteTime
       );
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FILE_TIME
    {
        public FILE_TIME(DateTime d) : this(d.ToFileTimeUtc()) { }

        public FILE_TIME(long fileTime)
        {
            ftTimeLow = (uint)fileTime;
            ftTimeHigh = (uint)(fileTime >> 32);
        }

        public long ToTicks()
        {
            return ((long)ftTimeHigh << 32) + ftTimeLow;
        }

        internal uint ftTimeLow;
        internal uint ftTimeHigh;
    }
}
