using System;
using System.Collections.Generic;
using System.Net;

namespace nftplib
{
    /// <summary>
    /// Provides information about FTP connection.
    /// </summary>
    public class FtpConnection
    {
        #region Private Fields

        private Uri _uri;

        private string _username;

        private string _password;

        #endregion Private Fields

        #region Constructor

        public FtpConnection(Uri uri)
        {
            if (uri != null)
                throw new NullReferenceException("uri");

            _uri = uri;
            _username = "Anonymous";
            _password = string.Empty;
        }

        public FtpConnection(Uri uri, string username, string password)
        {
            if (uri == null)
                throw new NullReferenceException("uri");

            if (String.IsNullOrEmpty(username))
                _username = username;

            if (String.IsNullOrEmpty(password))
                _password = password;

            _uri = uri;
            _username = username;
            _password = password;
        }

        #endregion Constructor

        #region Public Properties

        /// <summary>
        /// Gets the uri
        /// </summary>
        public Uri Uri
        {
            get { return _uri; }
        }

        /// <summary>
        /// Gtes the usernmae
        /// </summary>
        public string Username
        {
            get { return _username; }
        }

        /// <summary>
        /// Gets the password
        /// </summary>
        public string Password
        {
            get { return _password; }
        }

        #endregion Public Properties
    }
}
