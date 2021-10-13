using System;
using System.Diagnostics;

namespace Romi.Standard.Sockets.Net
{
    public class CloseReasonInfo
    {
        public static readonly CloseReasonInfo Default = new CloseReasonInfo();
        public static readonly CloseReasonInfo Empty = new CloseReasonInfo(false, "Empty");

        public string Reason;
        public string Message;
        public int CallingStackFrame;
        public bool Error;

        private const int DefaultRelativeCallingStackFrame = 2;

        public CloseReasonInfo()
            : this(false, "Calling Close")
        {
        }

        public CloseReasonInfo(Exception ex, int callingStackFrame = 0)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            Error = true;
            Reason = ex.GetType().Name;
            Message = ex.ToString();
            CallingStackFrame = callingStackFrame;
        }

        public CloseReasonInfo(bool error, string reason, string message = "", int callingStackFrame = 0)
        {
            Error = error;
            Reason = reason;
            Message = message;
            CallingStackFrame = callingStackFrame + DefaultRelativeCallingStackFrame;
        }

        public override string ToString()
        {
            var st = new StackTrace();
            return $"{Reason} ({Message}) from '{st.GetFrame(CallingStackFrame)}' (Error: {Error})";
        }
    }
}
