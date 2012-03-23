using System;
using System.Collections.Generic;
using System.Text;

namespace nftplib
{
    public class TimeoutException : Exception
    {
        public TimeoutException(string message)
            : base(message) { }

        public TimeoutException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
