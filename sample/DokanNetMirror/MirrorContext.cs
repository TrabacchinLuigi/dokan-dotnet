using System;
using System.IO;
using FileAccess = DokanNet.FileAccess;

namespace DokanNetMirror
{
    public class MirrorContext
    {
        public event Action<MirrorContext, CallResult> CallResultAdded;

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Int32 ProcessId { get; private set; }
        public String IdentityName { get; private set; }
        public String FileName { get; private set; }

        public FileAccess Access { get; private set; }
        public FileShare Share { get; private set; }
        public FileMode Mode { get; private set; }

        public FileStream FileStream { get; set; }

        public void AddCall(CallResult call)
        {
            CallResultAdded?.Invoke(this, call);
        }

        public MirrorContext(System.Security.Principal.WindowsIdentity identity, String fileName, Int32 processId, FileAccess access, FileShare share, FileMode mode)
        {
            IdentityName = identity?.Name;
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            ProcessId = processId;

            Mode = mode;
            Access = access;
            Share = share;
        }
    }
}
