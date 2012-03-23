using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;
using nftplib;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace JoomlaAudio
{
    public class JoomlaProxy
    {
        private IJoomla _joomla;

        public JoomlaProxy()
        {
            _joomla = XmlRpcProxyGen.Create<IJoomla>();

        }

        public Category[] GetCategories()
        {
            return _joomla.GetCategories("dummy", "USER", "PASS");
        }

        public string NewPost(string title, string category, string url, long size, DateTime created)
        {
            Post post = new Post();
            post.categories = new string[] { category };
            post.dateCreated = DateTime.Now;
            post.description = string.Format("{{enclose {0} {1} audio/mpeg}}", url, size);
            post.title = title;
            post.dateCreated = created;

            string result = _joomla.newPost("dummy", "USER", "PASS", post, true);

            return result;
        }

        public List<Mp3File> GetLastFiles(int top)
        {
            FtpClient c = new FtpClient(FTPServer, FTPServerUser, FTPServerPassw);
            //bool result = c.UploadFile(targetFilename, string.Format("{0}{1}", Config.FTPServerLocation, new FileInfo(targetFilename).Name));

            //List<string> files = new List<string>();

            //files.Sort();

            List<Mp3File> mp3s = new List<Mp3File>();
            foreach (string dir in FTPServerLocation)
            {
                List<string> files = c.GetDirectoryList(dir);
                files.Remove(".");
                files.Remove("..");
                files.Sort();
                //foreach (string f in )
                //    if (!f.Equals(".") && !f.Equals(".."))
                //        files.Add(dir + "/" + f);

                string dirname = dir.Replace("public_html/", "");

                for (int i = 0; i < 10; i++)
                {
                    string filename = files[files.Count - (i + 1)];

                    Mp3File mp3 = new Mp3File();
                    mp3.url = "http://www.example.com/" + dirname + "/" + filename;
                    mp3.size = c.GetFileSize(dir + "/" + filename);
                    mp3.naam = dirname + " - " + filename;

                    mp3s.Add(mp3);
                }
            }

            return mp3s;
        }

        public Mp3File UploadNewFile(string input, DateTime date, string titel, CategoryObj cat)
        {
            string filename = string.Format("{0}-{1}.mp3", date.ToString("yyyyMMdd"), titel.Replace(" ", "_").Replace(".", "").ToLowerInvariant());
            string targetfilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);

            EncodeMP3(input, targetfilename);

            FtpClient c = new FtpClient(FTPServer, FTPServerUser, FTPServerPassw);
            bool result = c.UploadFile(targetfilename, string.Format("public_html/{0}/{1}", GetFTPServerLocation(cat), filename));
            if (!result)
                throw new ApplicationException("Uploaden niet gelukt");

            Mp3File mp3 = new Mp3File();
            mp3.naam = filename;
            mp3.size = new FileInfo(targetfilename).Length;
            mp3.url = string.Format("{0}{1}/{2}", BaseWebUrl, GetFTPServerLocation(cat), filename);

            File.Delete(targetfilename);

            return mp3;
        }

        private void EncodeMP3(string srcFileName, string targetFileName)
        {
            if (!string.IsNullOrEmpty(srcFileName) && !string.IsNullOrEmpty(targetFileName))
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = LameEncoder;
                psi.Arguments = string.Format("-V9 -b 32 -h \"{0}\" \"{1}\"", srcFileName, targetFileName);
                psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                Process p = System.Diagnostics.Process.Start(psi);
                while (p.HasExited == false)
                {
                    Thread.Sleep(100);
                    Application.DoEvents();
                }
                p.Dispose();
            }
        }

        public static string LameEncoder
        {
            get
            {
                return System.IO.Path.Combine(Application.StartupPath, "lame.exe");
            }
        }

        private static string[] FTPServerLocation
        {
            get
            {
                return new string[] {"public_html/preken/samenkomsten"
                            , "public_html/preken/maranatha"
                            , "public_html/preken/jeugddiensten"
                            , "public_html/bijbelstudies"};
            }
        }

        private static string GetFTPServerLocation(CategoryObj cat)
        {
            switch (cat.ToString())
            {
                case "samenkomst bunschoten":
                    return "preken/samenkomsten";
                case "maranatha avond": 
                    return "preken/maranatha";
                case "jeugddienst": 
                    return "preken/jeugddiensten";
                case "bijbelstudie": 
                    return "bijbelstudies";
                case "samenkomst baarn":
                    return "preken/samenkomsten";
                default: 
                    return "preken/samenkomsten";
            }
        }

        private static string BaseWebUrl
        {
            get { return "http://www.example.com/"; }
        }

        private static string FTPServer
        {
            get { return "ftp://exampleftp.com"; }
        }

        private static string FTPServerUser
        {
            get { return "FTPUSER"; }
        }

        private static string FTPServerPassw
        {
            get { return "FTPPASS"; }
        }
    }

    public class Mp3File
    {
        public string url;
        public string naam;
        public long size;

        public override string ToString()
        {
            return string.Format("{0} ({1})", naam, size);
        }
    }

    public class CategoryObj
    {
        private Category _c;
        public CategoryObj(Category c)
        {
            _c = c;
        }

        public override string ToString()
        {
            return _c.title;
        }
    }

    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public struct Category
    {
        public string description;
        public string title;
        public string htmlUrl;
        public string rssUrl;
    }

    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public struct Post
    {
        public DateTime dateCreated;
        public string description;
        public string title;
        public string postid;
        public string[] categories;
    }

    [XmlRpcUrl("http://www.joomlaexample.com/xmlrpc/index.php")]
    public interface IJoomla : IXmlRpcProxy
    {
        [XmlRpcMethod("metaWeblog.getCategories")]
        Category[] GetCategories(string blogid, string username, string password);
        //The struct returned contains one struct for each category, containing the following elements: description, htmlUrl and rssUrl.

        [XmlRpcMethod("metaWeblog.newPost")]
        string newPost(string blogid, string username, string password, Post content, bool publish);

        //public void blogger.getUsersBlogs
        //public void blogger.getUserInfo
        //public void blogger.deletePost
        //public void blogger.getTemplate
        //public void metaWeblog.getUsersBlogs
        //public void metaWeblog.getUserInfo
        //public void metaWeblog.deletePost
        //public void metaWeblog.newPost
        //public void metaWeblog.editPost
        //public void metaWeblog.getPost
        //public void metaWeblog.getCategories
        //public void metaWeblog.getRecentPosts
        //public void metaWeblog.newMediaObject
        //public void joomla.searchSite
        //public void blogger.getPost
        //public void blogger.getRecentPosts
        //public void blogger.setTemplate
        //public void blogger.newPost
        //public void blogger.editPost
        //public void system.listMethods
        //public void system.methodHelp
        //public void system.methodSignature
        //public void system.multicall
        //public void system.getCapabilities
    }
}
