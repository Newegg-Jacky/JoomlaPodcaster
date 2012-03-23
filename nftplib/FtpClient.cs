using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace nftplib
{
    public class FtpClient
    {
        #region Private Constants

        private const string _dirErrorMsg = "Directory can't be null or empty.";

        private const string _fileErrorMsg = "File name can't be null or empty.";

        #endregion Private Constants

        #region Private Fields

        private FtpSession _session;

        private FtpConnection _connection;

        #endregion Private Fields

        #region Constructor

        public FtpClient(IPEndPoint ipEndPoint)
        {
            if (ipEndPoint == null)
                throw new NullReferenceException("ipEndPoint");

            Uri uri = new Uri(String.Format("ftp://{0}:{1}", ipEndPoint.Address, ipEndPoint.Port));

            if (uri.Scheme != Uri.UriSchemeFtp)
                throw new ArgumentException("ipEndPoint");

            _connection = new FtpConnection(uri);

            _session = new FtpSession(_connection);
        }

        public FtpClient(IPEndPoint ipEndPoint, string username, string password)
        {
            if (ipEndPoint == null)
                throw new NullReferenceException("ipEndPoint");

            Uri uri = new Uri(String.Format("ftp://{0}:{1}", ipEndPoint.Address, ipEndPoint.Port));

            if (uri.Scheme != Uri.UriSchemeFtp)
                throw new ArgumentException("ipEndPoint");

            if (String.IsNullOrEmpty(username))
                throw new ArgumentException("username");

            if (String.IsNullOrEmpty(password))
                throw new ArgumentException("password");

            _connection = new FtpConnection(uri, username, password);

            _session = new FtpSession(_connection);
        }

        public FtpClient(string url)
        {
            if (String.IsNullOrEmpty(url))
                throw new ArgumentException(url);

            Uri uri = new Uri(url);

            if (uri.Scheme != Uri.UriSchemeFtp)
                throw new ArgumentException("uri");

            _connection = new FtpConnection(uri);

            _session = new FtpSession(_connection);
        }

        public FtpClient(string url, string username, string password)
        {
            if (String.IsNullOrEmpty(url))
                throw new ArgumentException(url);

            if (String.IsNullOrEmpty(username))
                throw new ArgumentException("username");

            if (String.IsNullOrEmpty(password))
                throw new ArgumentException("password");

            Uri uri = new Uri(url);

            if (uri.Scheme != Uri.UriSchemeFtp)
                throw new ArgumentException("uri");

            _connection = new FtpConnection(uri, username, password);

            _session = new FtpSession(_connection);
        }

        #endregion Constructor

        #region Public Properties

        /// <summary>
        /// Gets current information on the connection.
        /// </summary>
        public FtpConnection Connection
        {
            get { return _connection; }
        }

        /// <summary>
        /// Gets the current directory on remote server.
        /// </summary>
        public string CurrentDirectory
        {
            get { return _session.CurrentDirecory; }
        }

        /// <summary>
        /// Gets or Sets a System.Boolean that specifies that an SSL connection should be used.
        /// </summary>
        public bool EnableSSL
        {
            get { return _session.EnableSSL; }
            set { _session.EnableSSL = value; }
        }

        /// <summary>
        /// Gets or Sets the proxy used to communicate with the FTP Server.
        /// </summary>
        public IWebProxy Proxy
        {
            get { return _session.Proxy; }
            set { _session.Proxy = value; }
        }

        /// <summary>
        /// Gets or Sets the number of milliseconds to wait a request.
        /// </summary>
        public int Timeout
        {
            get { return _session.Timeout; }
            set { _session.Timeout = value; }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Downloading file from remote server by use FTP protocol and saved to the local file.         
        /// Exceptions:
        ///     - ArgumentException
        ///     - FtpException
        /// </summary>
        /// <param name="remotePath">Remote path</param>
        /// <param name="path">Path on local computer</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool DownloadFile(string remotePath, string path)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentException("fileName");

            if (String.IsNullOrEmpty(remotePath))
                throw new ArgumentException("remoteFileName");

            if (File.Exists(path))
                throw new FtpException("File is already exists");

            if (!IsFileExists(remotePath))
                throw new FtpException("Remote file is not found.");

            bool result = false;

            using (FileStream fStream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                result = download(_session.GetURI(remotePath), fStream);                
            }

            return result;
        }


        /// <summary>
        /// Downloading file from remote server by use FTP protocol and saved in stream.        
        /// Exceptions:
        ///     - ArgumentException
        ///     - NullReferenceException
        ///     - FtpException
        /// </summary>
        /// <param name="remotePath">Remote path</param>
        /// <param name="stream">Data stream</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool DownloadFile(string remotePath, Stream stream)
        {
            if (stream == null)
                throw new NullReferenceException("stream");

            if (String.IsNullOrEmpty(remotePath))
                throw new ArgumentException("remoteFileName");

            if (!IsFileExists(remotePath))
                throw new FtpException("Remote file is not found.");

            return download(_session.GetURI(remotePath), stream);
        }

        /// <summary>
        /// Transfer file from local computer into of another computer.  
        /// Exceptions:
        ///     - ArgumentException
        ///     - FileNotFoundException
        ///     - FtpException
        /// </summary>
        /// <param name="path">Local path.</param>
        /// <param name="remotePath">Remote path.</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool UploadFile(string path, string remotePath)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentException("fileName");

            if (String.IsNullOrEmpty(remotePath))
                throw new ArgumentException("remoteFileName");

            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            if (IsFileExists(remotePath))
                throw new FtpException("Remote file is already exists");

            bool isSuccessfull = false;

            using (FileStream fStream = File.OpenRead(path))
            {
                try
                {
                    isSuccessfull = upload(fStream, _session.GetURI(remotePath));
                }
                catch
                {
                    isSuccessfull = false;
                }
                finally
                {
                    //ensure file closed
                    fStream.Close();
                }
            }
            return isSuccessfull;
        }

        /// <summary>
        /// Transfer stream data from local computer into of another computer.        
        /// </summary>
        /// Exceptions:
        ///     - ArgumentException
        ///     - NullReferenceException
        ///     - FtpException
        /// <param name="stream">Data stream.</param>
        /// <param name="remotePath">Remote path.</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool UploadFile(Stream stream, string remotePath)
        {
            if (stream == null)
                throw new NullReferenceException("stream");

            if (String.IsNullOrEmpty(remotePath))
                throw new ArgumentException("remoteFileName");

            if (IsFileExists(remotePath))
                throw new FtpException("Remote file is already exists");

            bool isSuccessfull = false;

            try
            {
                isSuccessfull = upload(stream, _session.GetURI(remotePath));
            }
            catch
            {
                isSuccessfull = false;
            }

            return isSuccessfull;
        }

        /// <summary>
        /// Gets file size on remote server.
        /// </summary>
        /// <param name="filename">File name on remote server.</param>
        /// <returns>File size.</returns>
        public long GetFileSize(string filename)
        {
            FtpWebRequest request = _session.GetRequest(_session.GetURI(filename));

            request.Method = WebRequestMethods.Ftp.GetFileSize;

            long size = _session.GetSize(request);

            return size;
        }

        /// <summary>
        /// Return TRUE, if file is already exists on remote server.
        /// If successfull thne return TRUE.
        /// </summary>
        /// Exceptions:
        ///     - ArgumentException
        /// <param name="filename">Remote file name.</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool IsFileExists(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(_fileErrorMsg);

            bool isSuccessfull = false;

            try
            {
                long size = GetFileSize(filename);
                isSuccessfull = true;
            }
            catch (FtpException ex)
            {
                if (ex.Message.Contains("550"))
                    isSuccessfull = false;

            }
            catch
            {
                throw;
            }

            return isSuccessfull;
        }

        /// <summary>
        /// Return TRUE, if directory is already exists on remote server.
        /// Exceptions:
        ///     - ArgumentException
        /// </summary>
        /// <param name="directory">Remote directory name.</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool IsDirectoryExists(string directory)
        {
            if (String.IsNullOrEmpty(directory))
                throw new ArgumentException(_dirErrorMsg);

            bool isSuccessfull = false;

            try
            {
                List<string> files = GetDirectoryList(directory);

                isSuccessfull = true;
            }
            catch (FtpException ex)
            {
                // error should contain 550 if not found
                if (ex.Message.Contains("550"))
                    isSuccessfull = false;
            }
            catch
            {
                throw;
            }

            return isSuccessfull;
        }

        /// <summary>
        /// Create folder on remote computer.
        /// Exceptions:
        ///     - ArgumentException
        /// </summary>        
        /// <param name="directory">Remote directory name.</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool CreateFolder(string directory)
        {
            if (String.IsNullOrEmpty(directory))
                throw new ArgumentException(_dirErrorMsg);

            bool isSuccessfull = false;

            try
            {
                FtpWebRequest request = _session.GetRequest(_session.GetURI(directory));

                request.Method = WebRequestMethods.Ftp.MakeDirectory;

                _session.GetStringResponse(request);

                isSuccessfull = true;
            }
            catch
            {
                isSuccessfull = false;
            }

            return isSuccessfull;
        }

        /// <summary>
        /// Deleted folder on remote server.
        /// Exceptions:
        ///     - ArgumentException
        /// </summary>
        /// <param name="directory">Directory name on remote server.</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool DeleteFolder(string directory)
        {
            if (String.IsNullOrEmpty(directory))
                throw new ArgumentException(_dirErrorMsg);

            bool isSuccessfull = false;

            try
            {
                FtpWebRequest request = _session.GetRequest(_session.GetURI(directory));

                request.Method = WebRequestMethods.Ftp.RemoveDirectory;

                _session.GetStringResponse(request);

                isSuccessfull = true;
            }
            catch
            {
                isSuccessfull = false;
            }

            return isSuccessfull;
        }

        /// <summary>
        /// Deleted file on remote server.
        /// Exceptions:
        ///     - ArgumentException
        /// </summary>
        /// <param name="filename">File name on remote server.</param>
        /// <returns>If successfull then return TRUE.</returns>
        public bool DeleteFile(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(_fileErrorMsg);

            bool isSuccessfull = false;

            try
            {
                FtpWebRequest request =
                    _session.GetRequest(_session.GetURI(filename));

                request.Method = WebRequestMethods.Ftp.DeleteFile;

                _session.GetStringResponse(request);

                isSuccessfull = true;
            }
            catch
            {
                isSuccessfull = false;
            }

            return isSuccessfull;
        }

        /// <summary>
        /// Gets simple directory list on remote server by current directory.
        /// Exceptions:
        ///     - ArgumentException
        /// </summary>
        /// <returns>Returns directory list on remote server.</returns>
        public List<string> GetDirectoryList()
        {
            return GetDirectoryList(_session.GetURI(CurrentDirectory));
        }

        /// <summary>
        /// Gets simple directory list on remote server by directory name.
        /// Exceptions:
        ///     - ArgumentException
        /// </summary>
        /// <param name="directory">Directory name on remote server.</param>
        /// <returns>Returns directory list on remote server.</returns>
        public List<string> GetDirectoryList(string directory)
        {
            if (String.IsNullOrEmpty(directory))
                throw new ArgumentException(_dirErrorMsg);

            FtpWebRequest request = _session.GetRequest(_session.GetURI(directory));

            request.Method = WebRequestMethods.Ftp.ListDirectory;

            string str = _session.GetStringResponse(request);
            //replace CRLF to CR, remove last instance
            str = str.Replace("\r\n", "\r").TrimEnd('\r');

            //split the string into a list
            List<string> result = new List<string>();
            if (String.IsNullOrEmpty(str))
                return result;

            result.AddRange(str.Split('\r'));

            for (int i = 0; i < result.Count; i++)
            {
                string[] parseline = result[i].Split('/');
                if (parseline.Length == 0)
                    continue;
                else
                {
                    result[i] = parseline[parseline.Length - 1];
                }
            }

            return result;
        }

        /// <summary>
        /// Set current directory on remote server.
        /// Exceptions:
        ///     - ArgumentException
        /// </summary>
        /// <param name="directory">Directory name on remote server.</param>
        public void SetCurrentDirectory(string directory)
        {
            if (String.IsNullOrEmpty(directory))
                throw new ArgumentException(_dirErrorMsg);

            _session.CurrentDirecory = directory;
        }

        #endregion Public Methods

        #region Private Methods

        private bool upload(Stream sourceStream, string remoteUriPath)
        {

            FtpWebRequest request = _session.GetRequest(remoteUriPath);

            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.UseBinary = true;

            request.ContentLength = sourceStream.Length;

            const int bufferLength = 2048;
            byte[] buffer = new byte[bufferLength];
            int count = 0;
            int readBytes = 0;

            bool isSuccessfull = false;

            using (sourceStream)
            {
                try
                {
                    sourceStream.Position = 0;

                    using (Stream rs = request.GetRequestStream())
                    {
                        do
                        {
                            readBytes = sourceStream.Read(buffer, 0, bufferLength);
                            rs.Write(buffer, 0, readBytes);
                            count += readBytes;
                        }
                        while (readBytes != 0);

                        rs.Close();
                    }
                    isSuccessfull = true;
                }
                catch (Exception)
                {
                    isSuccessfull = false;
                }
                finally
                {
                    request = null;
                }
            }
            return isSuccessfull;
        }

        private bool download(string remoteUriPath, Stream stream)
        {
            FtpWebRequest request = _session.GetRequest(remoteUriPath);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.UseBinary = true;

            bool isSuccessfull = false;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    try
                    {
                        const int bufferLength = 2048;
                        byte[] buffer = new byte[bufferLength];
                        int count = 0;
                        int readBytes = 0;

                        do
                        {
                            readBytes = responseStream.Read(buffer, 0, bufferLength);
                            stream.Write(buffer, 0, readBytes);
                            count += readBytes;
                        } while (readBytes != 0);

                        responseStream.Close();
                        stream.Flush();

                        isSuccessfull = true;

                        responseStream.Close();
                    }
                    catch
                    {
                        isSuccessfull = false;
                    }
                }

                return isSuccessfull;
            }
        }

        #endregion Private Methods
    }
}
