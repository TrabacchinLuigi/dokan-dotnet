using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SilentWave.Utility
{
    public static class ShellNotifier
    {
        public static class Folder
        {
            public static void Updated(String path)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_ATTRIBUTES,
                    HChangeNotifyFlags.SHCNF_PATHW,
                    Marshal.StringToCoTaskMemAuto(path),
                    IntPtr.Zero);
            }
            public static void ContentChanged(String path)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_UPDATEDIR,
                    HChangeNotifyFlags.SHCNF_PATHA,
                    Marshal.StringToCoTaskMemAuto(path),
                    IntPtr.Zero);
            }
            public static void Created(String path)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_MKDIR,
                    HChangeNotifyFlags.SHCNF_PATHW,
                    Marshal.StringToCoTaskMemAuto(path),
                    IntPtr.Zero);
            }
            public static void Deleted(String path)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_RMDIR,
                    HChangeNotifyFlags.SHCNF_PATHW,
                    Marshal.StringToCoTaskMemAuto(path),
                    IntPtr.Zero);
            }
            public static void Renamed(String oldPath, String newPath)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_RENAMEFOLDER,
                    HChangeNotifyFlags.SHCNF_PATHW,
                    Marshal.StringToCoTaskMemAuto(oldPath),
                    Marshal.StringToCoTaskMemAuto(newPath));
            }
        }

        public static class File
        {
            public static void Updated(String path)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_ATTRIBUTES,
                    HChangeNotifyFlags.SHCNF_PATHW,
                    Marshal.StringToCoTaskMemAuto(path),
                    IntPtr.Zero);
            }
            public static void Created(String path)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_CREATE,
                    HChangeNotifyFlags.SHCNF_PATHW,
                    Marshal.StringToCoTaskMemAuto(path),
                    IntPtr.Zero);
            }
            public static void Deleted(String path)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_DELETE,
                    HChangeNotifyFlags.SHCNF_PATHW,
                    Marshal.StringToCoTaskMemAuto(path),
                    IntPtr.Zero);
            }
            public static void Renamed(String oldPath, String newPath)
            {
                SafeNativeMethods.SHChangeNotify(
                    HChangeNotifyEventID.SHCNE_RENAMEITEM,
                    HChangeNotifyFlags.SHCNF_PATHW,
                    Marshal.StringToCoTaskMemAuto(oldPath),
                    Marshal.StringToCoTaskMemAuto(newPath));
            }
        }




    }
}
