using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using DokanNet;
using DokanNet.Logging;
using static DokanNet.FormatProviders;
using FileAccess = DokanNet.FileAccess;
using SilentWave.IOExtensons;

namespace DokanNetMirror
{
    public class Mirror : IDokanOperations
    {
        public event Action<Mirror, MirrorContext> ContextCreated;
        private readonly string path;

        private const FileAccess DataAccess = FileAccess.ReadData | FileAccess.WriteData | FileAccess.AppendData |
                                              FileAccess.Execute |
                                              FileAccess.GenericExecute | FileAccess.GenericWrite |
                                              FileAccess.GenericRead;

        private const FileAccess DataWriteAccess = FileAccess.WriteData | FileAccess.AppendData |
                                                   FileAccess.Delete |
                                                   FileAccess.GenericWrite;

        private ConsoleLogger logger = new ConsoleLogger("[Mirror] ");

        public Mirror(string path)
        {
            if (!Directory.Exists(path))
                throw new ArgumentException(nameof(path));
            this.path = path;
        }

        private string GetPath(string fileName)
        {
            return path + fileName;
        }

        private NtStatus Trace(string method, string fileName, DokanFileInfo info, NtStatus result,
            params object[] parameters)
        {
#if TRACE
            var extraParameters = parameters != null && parameters.Length > 0
                ? ", " + string.Join(", ", parameters.Select(x => string.Format(DefaultFormatProvider, "{0}", x)))
                : string.Empty;

            logger.Debug(DokanFormat($"{method}('{fileName}', {info}{extraParameters}) -> {result}"));
#endif

            return result;
        }

        private NtStatus Trace(string method, string fileName, DokanFileInfo info,
            FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes,
            NtStatus result)
        {
#if TRACE
            logger.Debug(
                DokanFormat(
                    $"{method}('{fileName}', {info}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}"));
#endif

            return result;
        }

        #region Implementation of IDokanOperations

        public NtStatus CreateFile(string fileName, FileAccess access, FileShare share, FileMode mode,
            FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            if (fileName.EndsWith(".slog", StringComparison.OrdinalIgnoreCase)) return NtStatus.AccessDenied;
            var result = NtStatus.Success;
            var filePath = GetPath(fileName);
            var mirrorcontext = new MirrorContext(info.GetRequestor(), fileName, info.ProcessId, access, share, mode);
            ContextCreated?.Invoke(this, mirrorcontext);

            info.Context = mirrorcontext;
            return TryAndLog(mirrorcontext, () =>
            {
                if (info.IsDirectory)
                {
                    try
                    {
                        switch (mode)
                        {
                            case FileMode.Open:
                                if (!Directory.Exists(filePath))
                                {
                                    try
                                    {
                                        if (!File.GetAttributes(filePath).HasFlag(FileAttributes.Directory))
                                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                                attributes, NtStatus.NotADirectory);
                                    }
                                    catch (Exception)
                                    {
                                        return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                            attributes, DokanResult.FileNotFound);
                                    }
                                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.PathNotFound);
                                }

                                new DirectoryInfo(filePath).EnumerateFileSystemInfos().Any();
                                // you can't list the directory
                                break;

                            case FileMode.CreateNew:
                                if (Directory.Exists(filePath))
                                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.FileExists);

                                try
                                {
                                    File.GetAttributes(filePath).HasFlag(FileAttributes.Directory);
                                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.AlreadyExists);
                                }
                                catch (IOException)
                                {
                                }

                                Directory.CreateDirectory(GetPath(fileName));
                                break;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                            DokanResult.AccessDenied);
                    }
                }
                else
                {
                    var pathExists = true;
                    var pathIsDirectory = false;

                    var readWriteAttributes = (access & DataAccess) == 0;
                    var readAccess = (access & DataWriteAccess) == 0;

                    try
                    {
                        pathExists = (Directory.Exists(filePath) || File.Exists(filePath));
                        pathIsDirectory = File.GetAttributes(filePath).HasFlag(FileAttributes.Directory);
                    }
                    catch (IOException)
                    {
                    }

                    switch (mode)
                    {
                        case FileMode.Open:

                            if (pathExists)
                            {
                                if (readWriteAttributes || pathIsDirectory)
                                // check if driver only wants to read attributes, security info, or open directory
                                {
                                    if (pathIsDirectory && (access & FileAccess.Delete) == FileAccess.Delete
                                        && (access & FileAccess.Synchronize) != FileAccess.Synchronize)
                                        //It is a DeleteFile request on a directory
                                        return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                            attributes, DokanResult.AccessDenied);

                                    info.IsDirectory = pathIsDirectory;

                                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options,
                                        attributes, DokanResult.Success);
                                }
                            }
                            else
                            {
                                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                    DokanResult.FileNotFound);
                            }
                            break;

                        case FileMode.CreateNew:
                            if (pathExists)
                                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                    DokanResult.FileExists);
                            break;

                        case FileMode.Truncate:
                            if (!pathExists)
                                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                    DokanResult.FileNotFound);
                            break;
                    }

                    if (access.HasFlag(FileAccess.ReadData) || access.HasFlag(FileAccess.WriteData) || access.HasFlag(FileAccess.AppendData))
                    {
                        try
                        {
                            mirrorcontext.FileStream = new FileStream(filePath, mode, readAccess ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite, share, 4096, options);
                            if (pathExists && (mode == FileMode.OpenOrCreate
                                || mode == FileMode.Create))
                                result = DokanResult.AlreadyExists;

                            if (mode == FileMode.CreateNew || mode == FileMode.Create) //Files are always created as Archive
                                attributes |= FileAttributes.Archive;
                            File.SetAttributes(filePath, attributes);
                        }
                        catch (UnauthorizedAccessException) // don't have access rights
                        {
                            if (mirrorcontext.FileStream != null)
                            {
                                mirrorcontext.FileStream.Dispose();
                                //mirrorcontext.FileStream = null;
                            }
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.AccessDenied);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                DokanResult.PathNotFound);
                        }
                        catch (Exception ex)
                        {
                            var hr = (uint)Marshal.GetHRForException(ex);
                            switch (hr)
                            {
                                case 0x80070020: //Sharing violation
                                    return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                                        DokanResult.SharingViolation);
                                default:
                                    throw;
                            }
                        }
                    }
                }
                return Trace(nameof(CreateFile), fileName, info, access, share, mode, options, attributes,
                    result);
            }, nameof(CreateFile));
        }

        public void Cleanup(string fileName, DokanFileInfo info)
        {
#if TRACE
            Console.WriteLine(DokanFormat($"{nameof(Cleanup)}('{fileName}', {info} - entering"));
#endif
            var mirrorcontext = info.Context as MirrorContext;
            TryAndLog(mirrorcontext, () =>
            {
                mirrorcontext.FileStream?.Dispose();
                //mirrorcontext.RealContext = null;

                if (info.DeleteOnClose)
                {
                    if (info.IsDirectory)
                    {
                        Directory.Delete(GetPath(fileName));
                    }
                    else
                    {
                        File.Delete(GetPath(fileName));
                    }
                }
                Trace(nameof(Cleanup), fileName, info, DokanResult.Success);
            }, nameof(Cleanup));
        }

        public void CloseFile(string fileName, DokanFileInfo info)
        {
#if TRACE
            Console.WriteLine(DokanFormat($"{nameof(CloseFile)}('{fileName}', {info} - entering"));
#endif
            var mirrorcontext = info.Context as MirrorContext;
            TryAndLog(mirrorcontext, () =>
            {
                mirrorcontext.FileStream?.Dispose();
                //mirrorcontext.RealContext = null;

                Trace(nameof(CloseFile), fileName, info, DokanResult.Success);
                // could recreate cleanup code here but this is not called sometimes
            }, nameof(CloseFile));
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, (out int innerBytesRead) =>
            {
                var canRead = mirrorcontext?.FileStream?.CanRead ?? false;
                if (!canRead) // memory mapped read
                {
                    using (var stream = new FileStream(GetPath(fileName), FileMode.Open, System.IO.FileAccess.Read))
                    {
                        stream.Position = offset;
                        innerBytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                }
                else // normal read
                {
                    var stream = mirrorcontext.FileStream;
                    lock (stream) //Protect from overlapped read
                    {
                        stream.Position = offset;
                        innerBytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                }

                return Trace(nameof(ReadFile), fileName, info, DokanResult.Success, "out " + innerBytesRead.ToString(),
                    offset.ToString(CultureInfo.InvariantCulture));
            }, out bytesRead, nameof(ReadFile));
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, (out int innerBytesWritten) =>
            {
                if (mirrorcontext.FileStream == null)
                {
                    using (var stream = new FileStream(GetPath(fileName), FileMode.Open, System.IO.FileAccess.Write))
                    {
                        stream.Position = offset;
                        stream.Write(buffer, 0, buffer.Length);
                        innerBytesWritten = buffer.Length;
                    }
                }
                else
                {
                    var stream = mirrorcontext.FileStream;
                    lock (stream) //Protect from overlapped write
                    {
                        stream.Position = offset;
                        stream.Write(buffer, 0, buffer.Length);
                    }
                    innerBytesWritten = buffer.Length;
                }
                return Trace(nameof(WriteFile), fileName, info, DokanResult.Success, "out " + innerBytesWritten.ToString(),
                    offset.ToString(CultureInfo.InvariantCulture));
            }, out bytesWritten, nameof(WriteFile));
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, () =>
            {
                try
                {
                    mirrorcontext.FileStream.Flush();
                    return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.Success);
                }
                catch (IOException)
                {
                    return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.DiskFull);
                }
            }, nameof(FlushFileBuffers));

        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, (out FileInformation innerfileinfo) =>
            {
                // may be called with info.Context == null, but usually it isn't
                var filePath = GetPath(fileName);
                FileSystemInfo finfo = new FileInfo(filePath);
                if (!finfo.Exists)
                    finfo = new DirectoryInfo(filePath);

                innerfileinfo = new FileInformation
                {
                    FileName = fileName,
                    Attributes = finfo.Attributes,
                    CreationTime = finfo.CreationTime,
                    LastAccessTime = finfo.LastAccessTime,
                    LastWriteTime = finfo.LastWriteTime,
                    Length = (finfo as FileInfo)?.Length ?? 0,
                };
                return Trace(nameof(GetFileInformation), fileName, info, DokanResult.Success);
            }, out fileInfo, nameof(GetFileInformation));
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, (out IList<FileInformation> innerfiles) =>
            {
                // This function is not called because FindFilesWithPattern is implemented
                // Return DokanResult.NotImplemented in FindFilesWithPattern to make FindFiles called
                innerfiles = FindFilesHelper(fileName, "*");

                return Trace(nameof(FindFiles), fileName, info, DokanResult.Success);
            }, out files, nameof(FindFiles));
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, () =>
            {
                try
                {
                    File.SetAttributes(GetPath(fileName), attributes);
                    return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.Success, attributes.ToString());
                }
                catch (UnauthorizedAccessException)
                {
                    return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.AccessDenied, attributes.ToString());
                }
                catch (FileNotFoundException)
                {
                    return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.FileNotFound, attributes.ToString());
                }
                catch (DirectoryNotFoundException)
                {
                    return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.PathNotFound, attributes.ToString());
                }
            }, nameof(SetFileAttributes));
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
            DateTime? lastWriteTime, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, () =>
            {
                try
                {
                    if (mirrorcontext.FileStream != null)
                    {
                        mirrorcontext.FileStream.SafeFileHandle.SetFileTime(creationTime, lastAccessTime, lastWriteTime);
                    }
                    else
                    {
                        var filePath = GetPath(fileName);
                        if (creationTime.HasValue)
                            File.SetCreationTime(filePath, creationTime.Value);

                        if (lastAccessTime.HasValue)
                            File.SetLastAccessTime(filePath, lastAccessTime.Value);

                        if (lastWriteTime.HasValue)
                            File.SetLastWriteTime(filePath, lastWriteTime.Value);
                    }
                    return Trace(nameof(SetFileTime), fileName, info, DokanResult.Success, creationTime, lastAccessTime,
                        lastWriteTime);
                }
                catch (UnauthorizedAccessException)
                {
                    return Trace(nameof(SetFileTime), fileName, info, DokanResult.AccessDenied, creationTime, lastAccessTime,
                        lastWriteTime);
                }
                catch (FileNotFoundException)
                {
                    return Trace(nameof(SetFileTime), fileName, info, DokanResult.FileNotFound, creationTime, lastAccessTime,
                        lastWriteTime);
                }
            }, nameof(SetFileTime));
        }

        public NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, () =>
            {
                var filePath = GetPath(fileName);

                if (Directory.Exists(filePath))
                    return Trace(nameof(DeleteFile), fileName, info, DokanResult.AccessDenied);

                if (!File.Exists(filePath))
                    return Trace(nameof(DeleteFile), fileName, info, DokanResult.FileNotFound);

                if (File.GetAttributes(filePath).HasFlag(FileAttributes.Directory))
                    return Trace(nameof(DeleteFile), fileName, info, DokanResult.AccessDenied);

                return Trace(nameof(DeleteFile), fileName, info, DokanResult.Success);
                // we just check here if we could delete the file - the true deletion is in Cleanup
            }, nameof(DeleteFile));
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
            var mirrorcontext = info.Context as MirrorContext;
            return TryAndLog(mirrorcontext, () =>
            {
                return Trace(nameof(DeleteDirectory), fileName, info,
                    Directory.EnumerateFileSystemEntries(GetPath(fileName)).Any()
                        ? DokanResult.DirectoryNotEmpty
                        : DokanResult.Success);
                // if dir is not empty it can't be deleted
            }, nameof(DeleteDirectory));
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, () =>
            {
                var oldpath = GetPath(oldName);
                var newpath = GetPath(newName);

                mirrorContext.FileStream?.Dispose();
                //mirrorContext.RealContext = null;

                var exist = info.IsDirectory ? Directory.Exists(newpath) : File.Exists(newpath);

                try
                {

                    if (!exist)
                    {
                        //mirrorContext.RealContext = null;
                        if (info.IsDirectory)
                            Directory.Move(oldpath, newpath);
                        else
                            File.Move(oldpath, newpath);
                        return Trace(nameof(MoveFile), oldName, info, DokanResult.Success, newName,
                            replace.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (replace)
                    {
                        //mirrorContext.RealContext = null;

                        if (info.IsDirectory) //Cannot replace directory destination - See MOVEFILE_REPLACE_EXISTING
                            return Trace(nameof(MoveFile), oldName, info, DokanResult.AccessDenied, newName,
                                replace.ToString(CultureInfo.InvariantCulture));

                        File.Delete(newpath);
                        File.Move(oldpath, newpath);
                        return Trace(nameof(MoveFile), oldName, info, DokanResult.Success, newName,
                            replace.ToString(CultureInfo.InvariantCulture));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return Trace(nameof(MoveFile), oldName, info, DokanResult.AccessDenied, newName,
                        replace.ToString(CultureInfo.InvariantCulture));
                }
                return Trace(nameof(MoveFile), oldName, info, DokanResult.FileExists, newName,
                    replace.ToString(CultureInfo.InvariantCulture));
            }, nameof(MoveFile));
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, () =>
            {
                try
                {
                    mirrorContext.FileStream.SetLength(length);
                    return Trace(nameof(SetEndOfFile), fileName, info, DokanResult.Success,
                        length.ToString(CultureInfo.InvariantCulture));
                }
                catch (IOException)
                {
                    return Trace(nameof(SetEndOfFile), fileName, info, DokanResult.DiskFull,
                        length.ToString(CultureInfo.InvariantCulture));
                }
            }, nameof(SetEndOfFile));
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, () =>
            {
                try
                {
                    mirrorContext.FileStream.SetLength(length);
                    return Trace(nameof(SetAllocationSize), fileName, info, DokanResult.Success,
                        length.ToString(CultureInfo.InvariantCulture));
                }
                catch (IOException)
                {
                    return Trace(nameof(SetAllocationSize), fileName, info, DokanResult.DiskFull,
                        length.ToString(CultureInfo.InvariantCulture));
                }
            }, nameof(SetAllocationSize));
        }

        public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, () =>
            {
#if !NETCOREAPP1_0
                try
                {
                    mirrorContext.FileStream.Lock(offset, length);
                    return Trace(nameof(LockFile), fileName, info, DokanResult.Success,
                        offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
                }
                catch (IOException)
                {
                    return Trace(nameof(LockFile), fileName, info, DokanResult.AccessDenied,
                        offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
                }
#else
            // .NET Core 1.0 do not have support for FileStream.Lock
            return NtStatus.NotImplemented;
#endif
            }, nameof(LockFile));
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, () =>
            {
#if !NETCOREAPP1_0
                try
                {
                    mirrorContext.FileStream.Unlock(offset, length);
                    return Trace(nameof(UnlockFile), fileName, info, DokanResult.Success,
                        offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
                }
                catch (IOException)
                {
                    return Trace(nameof(UnlockFile), fileName, info, DokanResult.AccessDenied,
                        offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
                }
#else
            // .NET Core 1.0 do not have support for FileStream.Unlock
            return NtStatus.NotImplemented;
#endif
            }, nameof(UnlockFile));
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, DokanFileInfo info)
        {
            var dinfo = DriveInfo.GetDrives().Single(di => string.Equals(di.RootDirectory.Name, Path.GetPathRoot(path + "\\"), StringComparison.OrdinalIgnoreCase));

            freeBytesAvailable = dinfo.TotalFreeSpace;
            totalNumberOfBytes = dinfo.TotalSize;
            totalNumberOfFreeBytes = dinfo.AvailableFreeSpace;
            return Trace(nameof(GetDiskFreeSpace), null, info, DokanResult.Success, "out " + freeBytesAvailable.ToString(),
                "out " + totalNumberOfBytes.ToString(), "out " + totalNumberOfFreeBytes.ToString());
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
            out string fileSystemName, DokanFileInfo info)
        {
            volumeLabel = "DOKAN";
            fileSystemName = "NTFS";

            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch |
                       FileSystemFeatures.PersistentAcls | FileSystemFeatures.SupportsRemoteStorage |
                       FileSystemFeatures.UnicodeOnDisk;

            return Trace(nameof(GetVolumeInformation), null, info, DokanResult.Success, "out " + volumeLabel,
                "out " + features.ToString(), "out " + fileSystemName);
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections,
            DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, (out FileSystemSecurity innerSecurity) =>
            {
#if !NETCOREAPP1_0
                try
                {
                    innerSecurity = info.IsDirectory
                        ? (FileSystemSecurity)Directory.GetAccessControl(GetPath(fileName))
                        : File.GetAccessControl(GetPath(fileName));
                    return Trace(nameof(GetFileSecurity), fileName, info, DokanResult.Success, sections.ToString());
                }
                catch (UnauthorizedAccessException)
                {
                    innerSecurity = null;
                    return Trace(nameof(GetFileSecurity), fileName, info, DokanResult.AccessDenied, sections.ToString());
                }
#else
            // .NET Core 1.0 do not have support for Directory.GetAccessControl
            security = null;
            return NtStatus.NotImplemented;
#endif
            }, out security, nameof(GetFileSecurity));
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections,
            DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, () =>
            {
#if !NETCOREAPP1_0
                try
                {
                    if (info.IsDirectory)
                    {
                        Directory.SetAccessControl(GetPath(fileName), (DirectorySecurity)security);
                    }
                    else
                    {
                        File.SetAccessControl(GetPath(fileName), (FileSecurity)security);
                    }
                    return Trace(nameof(SetFileSecurity), fileName, info, DokanResult.Success, sections.ToString());
                }
                catch (UnauthorizedAccessException)
                {
                    return Trace(nameof(SetFileSecurity), fileName, info, DokanResult.AccessDenied, sections.ToString());
                }
#else
            // .NET Core 1.0 do not have support for Directory.SetAccessControl
            return NtStatus.NotImplemented;
#endif
            }, nameof(SetFileSecurity));
        }

        public NtStatus Mounted(DokanFileInfo info)
        {
            return Trace(nameof(Mounted), null, info, DokanResult.Success);
        }

        public NtStatus Unmounted(DokanFileInfo info)
        {
            return Trace(nameof(Unmounted), null, info, DokanResult.Success);
        }

        public NtStatus FindStreams(string fileName, IntPtr enumContext, out string streamName, out long streamSize,
            DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, (out string innerStreamName, out long innerStreamSize) =>
            {
                innerStreamName = string.Empty;
                innerStreamSize = 0;
                return Trace(nameof(FindStreams), fileName, info, DokanResult.NotImplemented, enumContext.ToString(),
                    "out " + innerStreamName, "out " + innerStreamSize.ToString());
            }, out streamName, out streamSize, nameof(FindStreams));
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, (out IList<FileInformation> innerStreams) =>
            {
                innerStreams = new FileInformation[0];
                return Trace(nameof(FindStreams), fileName, info, DokanResult.NotImplemented);
            }, out streams, nameof(FindStreams));
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files,
            DokanFileInfo info)
        {
            var mirrorContext = info.Context as MirrorContext;
            return TryAndLog(mirrorContext, (out IList<FileInformation> innerFiles) =>
            {
                innerFiles = FindFilesHelper(fileName, searchPattern);

                return Trace(nameof(FindFilesWithPattern), fileName, info, DokanResult.Success);
            }, out files, nameof(FindFilesWithPattern));
        }

        #endregion Implementation of IDokanOperations

        public IList<FileInformation> FindFilesHelper(string fileName, string searchPattern)
        {
            IList<FileInformation> files = new DirectoryInfo(GetPath(fileName))
                .EnumerateFileSystemInfos()
                .Where(finfo => DokanHelper.DokanIsNameInExpression(searchPattern, finfo.Name, true))
                .Select(finfo => new FileInformation
                {
                    Attributes = finfo.Attributes,
                    CreationTime = finfo.CreationTime,
                    LastAccessTime = finfo.LastAccessTime,
                    LastWriteTime = finfo.LastWriteTime,
                    Length = (finfo as FileInfo)?.Length ?? 0,
                    FileName = finfo.Name
                }).ToArray();

            return files;
        }

        delegate NtStatus DelegateResult();
        delegate NtStatus DelegateResult<T>(out T out1);
        delegate NtStatus DelegateResult<T, S>(out T out1, out S out2);

        private NtStatus TryAndLog<T, S>(MirrorContext mirrorContext, DelegateResult<T, S> func, out T out1, out S out2, string method = null)
        {
            var call = new CallResult(method);
            try
            {
                var result = func(out out1, out out2);
                call.Result = result;
                return result;
            }
            catch (Exception ex)
            {
                call.Exception = ex;
                throw;
            }
            finally
            {
                mirrorContext.AddCall(call);
                call.Ended = DateTimeOffset.Now;
            }
        }

        private NtStatus TryAndLog<T>(MirrorContext mirrorContext, DelegateResult<T> func, out T something, string method = null)
        {
            return TryAndLog<T, int>(mirrorContext, (out T innerOut1, out int innerDiscard) =>
            {
                innerDiscard = 0;
                return func(out innerOut1);
            }, out something, out int discard, method);
        }

        private NtStatus TryAndLog(MirrorContext mirrorContext, Func<NtStatus> func, string method = null)
        {
            return TryAndLog<int>(mirrorContext, (out int innerDiscard) =>
            {
                innerDiscard = 0;
                return func();
            }, out int discard, method);
        }

        private void TryAndLog(MirrorContext mirrorContext, Action action, string method = null)
        {
            TryAndLog(mirrorContext, () =>
            {
                action();
                return 0; // discard it
            }, method);
        }
    }
}
