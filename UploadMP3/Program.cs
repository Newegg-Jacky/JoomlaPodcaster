using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;

namespace UploadMP3
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string filename = "";
            if (args.Length > 0)
                filename = args[0];

            Form1 f = new Form1(filename);
            StockIcons sicons = new StockIcons();
            f.Icon = sicons.AudioFiles.Icon;
            Application.Run(f);
        }
    }
}
