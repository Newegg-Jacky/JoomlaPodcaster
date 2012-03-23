using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace UploadMP3
{
    public class Config
    {
        
        public static string LameEncoder
        {
            get
            {
                return System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "lame.exe");
            }
        }

        public static string TargetBitrate
        {
            get { return "32"; }
        }

        public static string FTPServer
        {
            get { return "ftp://exampleftp.com"; }
        }

        public static string FTPServerUser
        {
            get { return "USER"; }
        }

        public static string FTPServerPassw
        {
            get { return "PASS"; }
        }

        public static string FTPServerLocation
        {
            get { return "public_html/preken/samenkomsten/"; }
        }
        /*
                    <add key="lameencoder" value=""/>
            <add key="targetbitrate" value="32"/>
            <add key="ftpserver" value=""/>
            <add key="ftpserver-user" value=""/>
            <add key="ftpserver-passw" value=""/>
            <add key="ftpserver-location" value=""/>
         */
    }
}
