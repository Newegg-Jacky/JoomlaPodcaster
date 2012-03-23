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
using Microsoft.WindowsAPICodePack.Taskbar;

namespace JoomlaAudio
{
    public partial class Form1 : Form
    {
        private JoomlaProxy _proxy;
        private Form2 _bezigForm;

        public Form1()
        {
            InitializeComponent();

            dateTimePicker2.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 45, 0);

            _proxy = new JoomlaProxy();

            foreach (Category c in _proxy.GetCategories())
            {
                if (c.title.Contains("audio"))
                    comboBox1.Items.Add(new CategoryObj(c));
            }

            foreach (Mp3File mp3 in _proxy.GetLastFiles(10))
                listBox1.Items.Add(mp3);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string inputfile = openFileDialog1.FileName;
            DateTime created = dateTimePicker1.Value;
            DateTime createdtime = dateTimePicker2.Value;
            string titel = textBox1.Text;
            Mp3File mp3 = listBox1.SelectedItem as Mp3File;
            CategoryObj c = comboBox1.SelectedItem as CategoryObj;

            if (!string.IsNullOrEmpty(openFileDialog1.FileName) && System.IO.File.Exists(openFileDialog1.FileName))
            {
                Thread t = new Thread(new ThreadStart(delegate()
                    {
                        try
                        {
                            SetProgress(0);
                            mp3 = _proxy.UploadNewFile(inputfile, created, titel, c);
                            SetProgress(50);
                            _proxy.NewPost(titel, c.ToString(), mp3.url, mp3.size, new DateTime(created.Year, created.Month, created.Day, createdtime.Hour, createdtime.Minute, createdtime.Second));
                        }
                        catch (Exception exc)
                        {
                            CloseBezig(exc);
                        }
                        SetProgress(100);
                        CloseBezig();
                    }));
                t.Start();
            }
            else if (mp3 != null && c != null)
            {
                Thread t = new Thread(new ThreadStart(delegate()
                {
                    try
                    {
                        _proxy.NewPost(titel, c.ToString(), mp3.url, mp3.size, new DateTime(created.Year, created.Month, created.Day, createdtime.Hour, createdtime.Minute, createdtime.Second));
                    }
                    catch (Exception exc)
                    {
                        CloseBezig(exc);
                    } 
                    CloseBezig();
                }));
                t.Start();
            }

            _bezigForm = new Form2();
            _bezigForm.ShowDialog(this);
        }

        private delegate void DSetProgress(int value);
        private void SetProgress(int value)
        {
            if (this.InvokeRequired)
            {
                DSetProgress d = new DSetProgress(SetProgress);
                this.Invoke(d, new object[] { value });
            }
            else
            {
                if (TaskbarManager.IsPlatformSupported)
                    TaskbarManager.Instance.SetProgressValue(value, 100);
            }
        }

        public delegate void DCloseBezig();
        public void CloseBezig()
        {
            if (this.InvokeRequired)
            {
                DCloseBezig d = new DCloseBezig(CloseBezig);
                this.Invoke(d, new object[] { });
            }
            else
            {
                if (_bezigForm != null)
                    _bezigForm.Close();
            }
        }

        public delegate void DCloseBezigWithExc(Exception exc);
        public void CloseBezig(Exception exc)
        {
            if (this.InvokeRequired)
            {
                DCloseBezigWithExc d = new DCloseBezigWithExc(CloseBezig);
                this.Invoke(d, new object[] { exc });
            }
            else
            {
                if (_bezigForm != null)
                    _bezigForm.Close();

                MessageBox.Show("Er is een fout opgetreden: "+exc.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                label5.Text = openFileDialog1.FileName;
            }
        }
    }
}

