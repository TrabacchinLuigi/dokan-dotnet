using System;
using DokanNet;

namespace DokanNetMirror
{
    public class CallResult
    {
        public DateTimeOffset Created { get; private set; }
        public DateTimeOffset Ended { get; set; }
        public string Method { get; private set; }
        public NtStatus? Result { get; set; }
        public Exception Exception { get; set; }

        public CallResult(String method)
        {
            Method = method;
            Created = DateTimeOffset.Now;
        }
    }
}
