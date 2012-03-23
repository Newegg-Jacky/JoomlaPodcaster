using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

namespace XmlRpcTest
{
    class Program
    {
        static void Main(string[] args)
        {
            IJoomla proxy = XmlRpcProxyGen.Create<IJoomla>();

            Category[] cats = proxy.GetCategories("dummy", "USER", "PASS");
            StringBuilder b = new StringBuilder();

            foreach (var c in cats)
                b.AppendLine(c.title);

            Post post = new Post();
            post.categories = new string[] { "samenkomst (audio)" };
            post.dateCreated = DateTime.Now;
            post.description = "{enclose iets.mp3 1 audio/mpeg}";
            post.title = "test";

            string result = proxy.newPost("dummy", "USER", "PASS", post, true);
        }

        private static string Introspect(IJoomla proxy)
        {
            string[] methods = proxy.SystemListMethods();
            StringBuilder b = new StringBuilder();
            b.AppendLine("[XmlRpcUrl(\"http://www.joomlaexample.com/joomla15/xmlrpc/index.php\")]");
            b.AppendLine("public interface IJoomla : IXmlRpcProxy");
            b.AppendLine("{");

            foreach (string m in methods)
            {
                try
                {
                    object[] os = proxy.SystemMethodSignature(m);
                    b.AppendLine("public void " + m);
                }
                catch
                {
                    b.AppendLine("//public void " + m);
                }
            }
            b.AppendLine("}");
            return b.ToString();
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

    [XmlRpcUrl("http://www.joomlaexample.com/joomla15/xmlrpc/index.php")]
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
