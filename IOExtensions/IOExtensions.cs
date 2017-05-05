using System;

namespace SilentWave.IOExtensons
{
    public static class IOExtensions
    {
        //static Type TWin32Native;
        //static Type TFILE_TIME;
        //static  IOExtensions()
        //{
        //    TWin32Native = Type.GetType("Microsoft.Win32.Win32Native");
        //    TFILE_TIME = Type.GetType("Microsoft.Win32.Win32Native.FILE_TIME");
        //}

        public static void SetFileTime(
            this Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
            DateTime? creationTime,
            DateTime? lastAccessTime,
            DateTime? lastWriteTime)
        {
            SetFileTimeUtc(
                hFile,
                creationTime?.ToUniversalTime(),
                lastAccessTime?.ToUniversalTime(),
                lastWriteTime?.ToUniversalTime());
        }

        public static void SetFileTimeUtc(
            this Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
            DateTime? creationTime,
            DateTime? lastAccessTime,
            DateTime? lastWriteTime)
        {
            unsafe
            {
                FILE_TIME ct, at, wt;
                ct = creationTime.HasValue ? new FILE_TIME(creationTime.Value) : default(FILE_TIME);
                at = lastAccessTime.HasValue ? new FILE_TIME(lastAccessTime.Value) : default(FILE_TIME);
                wt = lastWriteTime.HasValue ? new FILE_TIME(lastWriteTime.Value) : default(FILE_TIME);
                var nulltp = (FILE_TIME*)null;
                var ctp = creationTime.HasValue ? &ct : nulltp;
                var atp = lastAccessTime.HasValue ? &at : nulltp;
                var wtp = lastWriteTime.HasValue ? &wt : nulltp;
                var r = UnsafeNativeMethods.SetFileTime(hFile, ctp, atp, wtp);
                if (!r)
                {
                    int errorCode = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    Error.WinIOError(errorCode, "");
                }
            }

            //unsafe {
            //    Activator.CreateInstance(TFILE_TIME, creationTimeUtc.ToFileTimeUtc())
            //    var fileTime = new Win32Native.FILE_TIME();
            //    var SetFileTime = TWin32Native.GetMethod("SetFileTime");
            //    bool r = SetFileTime.Invoke(null, handle, &fileTime, null, null);
            //    if (!r)
            //    {
            //        int errorCode = Marshal.GetLastWin32Error();
            //        __Error.WinIOError(errorCode, path);
            //    }
            //}
        }
    }
}
