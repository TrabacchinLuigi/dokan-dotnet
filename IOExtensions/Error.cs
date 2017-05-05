using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace SilentWave.IOExtensons
{
    internal class Error
    {
        /// <remarks>
        /// After calling GetLastWin32Error(), it clears the last error field,
        /// so you must save the HResult and pass it to this method.  This method
        /// will determine the appropriate exception to throw dependent on your 
        /// error, and depending on the error, insert a string into the message 
        /// gotten from the ResourceManager.
        /// </remarks>
        internal static void WinIOError(int errorCode, String maybeFullPath)
        {
            // This doesn't have to be perfect, but is a perf optimization.
            bool isInvalidPath = errorCode == Win32Native.ERROR_INVALID_NAME || errorCode == Win32Native.ERROR_BAD_PATHNAME;

            switch (errorCode)
            {
                case Win32Native.ERROR_FILE_NOT_FOUND:
                    if (maybeFullPath.Length == 0)
                        throw new FileNotFoundException("File not found");
                    else
                        throw new FileNotFoundException($"File {maybeFullPath} not found", maybeFullPath);

                case Win32Native.ERROR_PATH_NOT_FOUND:
                    if (maybeFullPath.Length == 0)
                        throw new DirectoryNotFoundException("Path not found");
                    else
                        throw new DirectoryNotFoundException($"Path {maybeFullPath} not fond");

                case Win32Native.ERROR_ACCESS_DENIED:
                    if (maybeFullPath.Length == 0)
                        throw new UnauthorizedAccessException("Unauthorized access");
                    else
                        throw new UnauthorizedAccessException($"Unauthorized access to {maybeFullPath}");

                case Win32Native.ERROR_ALREADY_EXISTS:
                    if (maybeFullPath.Length == 0)
                        goto default;
                    throw new IOException($"File {maybeFullPath} already exist", Win32Native.MakeHRFromErrorCode(errorCode));

                case Win32Native.ERROR_FILENAME_EXCED_RANGE:
                    throw new PathTooLongException("Path too long");

                case Win32Native.ERROR_INVALID_DRIVE:
                    throw new DriveNotFoundException($"Drive not found on {maybeFullPath}");

                case Win32Native.ERROR_INVALID_PARAMETER:
                    throw new IOException($"Invalid parameter on {maybeFullPath}", Win32Native.MakeHRFromErrorCode(errorCode) );

                case Win32Native.ERROR_SHARING_VIOLATION:
                    if (maybeFullPath.Length == 0)
                        throw new IOException("Sharing violation", Win32Native.MakeHRFromErrorCode(errorCode));
                    else
                        throw new IOException($"Sharing violation on {maybeFullPath}", Win32Native.MakeHRFromErrorCode(errorCode));

                case Win32Native.ERROR_FILE_EXISTS:
                    if (maybeFullPath.Length == 0)
                        goto default;
                    throw new IOException($"File {maybeFullPath} not exist", Win32Native.MakeHRFromErrorCode(errorCode));

                case Win32Native.ERROR_OPERATION_ABORTED:
                    throw new OperationCanceledException();

                default:
                    throw new IOException($"Unknown error on {maybeFullPath}", Win32Native.MakeHRFromErrorCode(errorCode));
            }
        }



        static class Win32Native
        {
            // Use this to translate error codes like the above into HRESULTs like
            // 0x80070006 for ERROR_INVALID_HANDLE
            internal static int MakeHRFromErrorCode(int errorCode)
            {
               // BCLDebug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!");
                return unchecked(((int)0x80070000) | errorCode);
            }

            // Error codes from WinError.h
            internal const int ERROR_SUCCESS = 0x0;
            internal const int ERROR_INVALID_FUNCTION = 0x1;
            internal const int ERROR_FILE_NOT_FOUND = 0x2;
            internal const int ERROR_PATH_NOT_FOUND = 0x3;
            internal const int ERROR_ACCESS_DENIED = 0x5;
            internal const int ERROR_INVALID_HANDLE = 0x6;
            internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
            internal const int ERROR_INVALID_DATA = 0xd;
            internal const int ERROR_INVALID_DRIVE = 0xf;
            internal const int ERROR_NO_MORE_FILES = 0x12;
            internal const int ERROR_NOT_READY = 0x15;
            internal const int ERROR_BAD_LENGTH = 0x18;
            internal const int ERROR_SHARING_VIOLATION = 0x20;
            internal const int ERROR_NOT_SUPPORTED = 0x32;
            internal const int ERROR_FILE_EXISTS = 0x50;
            internal const int ERROR_INVALID_PARAMETER = 0x57;
            internal const int ERROR_BROKEN_PIPE = 0x6D;
            internal const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
            internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
            internal const int ERROR_INVALID_NAME = 0x7B;
            internal const int ERROR_BAD_PATHNAME = 0xA1;
            internal const int ERROR_ALREADY_EXISTS = 0xB7;
            internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
            internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
            internal const int ERROR_NO_DATA = 0xE8;
            internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
            internal const int ERROR_MORE_DATA = 0xEA;
            internal const int ERROR_DIRECTORY = 0x10B;
            internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
            internal const int ERROR_NOT_FOUND = 0x490;          // 1168; For IO Cancellation
            internal const int ERROR_NO_TOKEN = 0x3f0;
            internal const int ERROR_DLL_INIT_FAILED = 0x45A;
            internal const int ERROR_NON_ACCOUNT_SID = 0x4E9;
            internal const int ERROR_NOT_ALL_ASSIGNED = 0x514;
            internal const int ERROR_UNKNOWN_REVISION = 0x519;
            internal const int ERROR_INVALID_OWNER = 0x51B;
            internal const int ERROR_INVALID_PRIMARY_GROUP = 0x51C;
            internal const int ERROR_NO_SUCH_PRIVILEGE = 0x521;
            internal const int ERROR_PRIVILEGE_NOT_HELD = 0x522;
            internal const int ERROR_NONE_MAPPED = 0x534;
            internal const int ERROR_INVALID_ACL = 0x538;
            internal const int ERROR_INVALID_SID = 0x539;
            internal const int ERROR_INVALID_SECURITY_DESCR = 0x53A;
            internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
            internal const int ERROR_CANT_OPEN_ANONYMOUS = 0x543;
            internal const int ERROR_NO_SECURITY_ON_OBJECT = 0x546;
            internal const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;
        }
    }
}
