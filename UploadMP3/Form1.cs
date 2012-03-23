using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using nftplib;
using System.IO;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Runtime.InteropServices;

namespace UploadMP3
{
    public partial class Form1 : Form
    {
        private string _filename;
        public Form1(string filename)
        {
            InitializeComponent();

            if(string.IsNullOrEmpty(filename))
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Multiselect = false;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    filename = dialog.FileName;
            }
            if (!string.IsNullOrEmpty(filename) && System.IO.File.Exists(filename))
            {
                _filename = filename;
                label1.Text = filename;
                textBox1.Text = DateTime.Now.ToString("yyyyMMdd");
            }
            else
            {
                MessageBox.Show("Geen bestand geselecteerd");
                this.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text) || string.IsNullOrEmpty(textBox2.Text))
                MessageBox.Show("Vul datum en voorganger in!");
            else
            {
                Thread t = new Thread(new ThreadStart(
                    delegate()
                    {
                        string targetfilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                                                             , string.Format("{0}-{1}.mp3", textBox1.Text, textBox2.Text.Replace(" ", "_").Replace(".","").ToLowerInvariant()));

                        try
                        {
                            EnableButton(false);
                            SetProgress(0);
                            WriteStatus("mp3 wordt gemaakt...");
                            
                            Mp3Encode(_filename, targetfilename);

                            SetProgress(40);
                            WriteStatus("mp3 wordt geupload...");
                            
                            UploadFile(targetfilename);
                            
                            SetProgress(80);

                            ShowHtml(textBox1.Text, textBox2.Text, textBox3.Text, new FileInfo(targetfilename).Name);
                        }
                        catch (Exception exc)
                        {
                            ShowException(exc);
                        }
                        finally
                        {
                            if (File.Exists(targetfilename))
                            {
                                WriteStatus("mp3 wordt verwijderd...");
                                File.Delete(targetfilename);
                            }

                            SetProgress(100);
                            WriteStatus("");

                            EnableButton(true);
                        }
                    }));
                t.Start();
            }
        }

        private void Mp3Encode(string srcFileName, string targetFileName)
        {
            if (!string.IsNullOrEmpty(srcFileName) && !string.IsNullOrEmpty(targetFileName))
            {
                try
                {
                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                    psi.FileName = Config.LameEncoder;
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
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public void UploadFile(string targetFilename)
        {
            FtpClient c = new FtpClient(Config.FTPServer, Config.FTPServerUser, Config.FTPServerPassw);
            bool result = c.UploadFile(targetFilename, string.Format("{0}{1}", Config.FTPServerLocation, new FileInfo(targetFilename).Name));
            if (!result)
                throw new ApplicationException("Uploaden niet gelukt");
        }

        public delegate void DSetProgress(int value);
        public void SetProgress(int value)
        {
            if (this.InvokeRequired)
            {
                DSetProgress d = new DSetProgress(SetProgress);
                this.Invoke(d, new object[] { value });
            }
            else
            {
                this.toolStripProgressBar1.Value = value;
                if(TaskbarManager.IsPlatformSupported)
                    TaskbarManager.Instance.SetProgressValue(value, this.toolStripProgressBar1.Maximum);
            }
        }

        public delegate void DWriteStatus(string st);
        public void WriteStatus(string st)
        {
            if (this.InvokeRequired)
            {
                DWriteStatus d = new DWriteStatus(WriteStatus);
                this.Invoke(d, new object[] { st });
            }
            else
            {
                this.toolStripStatusLabel1.Text = st;
            }
        }

        public delegate void DEnableButton(bool b);
        public void EnableButton(bool b)
        {
            if (this.InvokeRequired)
            {
                DEnableButton d = new DEnableButton(EnableButton);
                this.Invoke(d, new object[] { b });
            }
            else
            {
                this.button1.Enabled = b;
            }
        }

        public delegate void DShowException(Exception e);
        public void ShowException(Exception e)
        {
            if (this.InvokeRequired)
            {
                DShowException d = new DShowException(ShowException);
                this.Invoke(d, new object[] { e });
            }
            else
            {
                MessageBox.Show(e.ToString());
            }
        }

        public delegate void DShowHtml(string datum, string voorganger, string titel, string filename);
        public void ShowHtml(string datum, string voorganger, string titel, string filename)
        {
            if (this.InvokeRequired)
            {
                DShowHtml d = new DShowHtml(ShowHtml);
                this.Invoke(d, new object[] { datum, voorganger, titel, filename });
            }
            else
            {
                HtmlForm html = new HtmlForm(datum, voorganger, titel, filename);
                html.Show();
            }
        }

    }
}
