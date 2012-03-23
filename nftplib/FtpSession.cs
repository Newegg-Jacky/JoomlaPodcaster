using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;

namespace nftplib
{
    /// <summary>
    /// Provides access to Ftp Server
    /// </summary>
    internal class FtpSession
    {
        #region Private Fields

        private FtpConnection _connection;

        private NetworkCredential _credential;

        private string _currentDir = string.Empty;

        private bool _enableSSL = false;

        private IWebProxy _proxy = null;

        private int _timeout;

        #endregion Private Fields

        #region Constructor

        public FtpSession(FtpConnection connection)
        {
            _connection = connection;
            _credential = new NetworkCredential(_connection.Username, _connection.Password);
        }

        #endregion Constructor

        #region Public Properties

        /// <summary>
        /// Gets or Sets current directory.
        /// </summary>
        public string CurrentDirecory
        {
            get { return _currentDir; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("Directory can't be null or empty.");

                if (value.StartsWith("/"))
                    throw new ArgumentException("Directory name can't be start of /");

                if (value.EndsWith("/"))
                    throw new ArgumentException("Directory name can't be end of /");

                _currentDir = String.Format("{0}/{1}", _connection.Uri, value);
            }
        }

        /// <summary>
        /// Gets or Sets a System.Boolean that specifies that an SSL connection should be used.
        /// </summary>
        public bool EnableSSL
        {
            get { return _enableSSL; }
            set { _enableSSL = value; }
        }

        /// <summary>
        /// Gets or Sets the proxy used to communicate with the FTP Server.
        /// </summary>
        public IWebProxy Proxy
        {
            get { return _proxy; }
            set { _proxy = value; }
        }

        /// <summary>
        /// Gets or Sets the number of milliseconds to wait a request.
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Gets FTP request by uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>FTP web request</returns>
        public FtpWebRequest GetRequest(string uri)
        {
            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uri);

            request.Credentials = _credential;
            request.EnableSsl = _enableSSL;
            request.KeepAlive = true;
            request.UsePassive = true;
            request.Proxy = _proxy;

            return request;
        }

        /// <summary>
        /// Gets response string by FtpWebRequest.
        /// Exceptions:
        ///     - FtpException
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Response string</returns>
        public string GetStringResponse(FtpWebRequest request)
        {
            string result = string.Empty;
            try
            {
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    long size = response.ContentLength;
                    using (Stream datastream = response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(datastream, System.Text.Encoding.UTF8))
                        {
                            result = sr.ReadToEnd();
                            sr.Close();
                        }

                        datastream.Close();
                    }

                    response.Close();
                }
            }
            catch (WebException ex)
            {
                throw new FtpException(ex.Message, ex);
            }

            return result;
        }

        /// <summary>
        /// Gets file length by FtpWebRequest.
        /// Exceptions:
        ///     - FtpException
        /// </summary>
        /// <param name="request"></param>
        /// <returns>File length</returns>
        public long GetSize(FtpWebRequest request)
        {
            long size;
            try
            {
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    size = response.ContentLength;
                    response.Close();
                }
            }
            catch (WebException ex)
            {
                throw new FtpException(ex.Message, ex);
            }

            return size;
        }

        /// <summary>
        /// Gets uri by directiry name.
        /// Exceptions:
        ///     - ArgumentException
        /// </summary>
        /// <param name="directory">Directory name.</param>
        /// <returns>URI on remote server.</returns>
        public string GetURI(string directory)
        {
            if (!String.IsNullOrEmpty(directory))
            {
                try
                {
                    Uri uriDir = new Uri(directory);
                    if (uriDir.Scheme == Uri.UriSchemeFtp)
                        return directory;
                }
                catch (UriFormatException)
                { }
            }

            if (directory.StartsWith("/"))
                throw new ArgumentException("Directory name can't be start of /");

            if (directory.EndsWith("/"))
                throw new ArgumentException("Directory name can't be end of /");

            string uri;

            if (!String.IsNullOrEmpty(_currentDir))
                uri = String.Format("{0}//{1}", _currentDir, directory);
            else
                uri = String.Format("{0}//{1}", _connection.Uri, directory);

            return uri;
        }

        #endregion Public Methods

        
    }
}
