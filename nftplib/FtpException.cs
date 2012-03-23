using System;
using System.Collections.Generic;
using System.Text;

namespace nftplib
{
    public class FtpException : Exception
    {
        public FtpException(string message)
            : base(message) { }

        public FtpException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
