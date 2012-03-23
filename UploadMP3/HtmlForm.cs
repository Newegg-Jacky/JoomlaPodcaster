using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UploadMP3
{
    public partial class HtmlForm : Form
    {
        public HtmlForm(string datum, string voorganger, string titel, string filename)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(titel))
            {
                textBox1.Text =
                     string.Format(
    @"<tr>
  <td>{0}</td>
  <td>&nbsp;</td>
  <td>
    <p>{1}</p>
    <p>
      <a href=""http://www.dereddingsarkmedia.nl/preken/samenkomsten/{2}"">{3}</a>
      <br />
    </p>
  </td>
</tr>", FormatDatum(datum), voorganger, filename, titel);
            }
            else
            {
                textBox1.Text =
                     string.Format(
    @"<tr>
  <td>{0}</td>
  <td></td>
  <td>
    <p>
      <a href=""http://www.dereddingsarkmedia.nl/preken/samenkomsten/{1}"">{2}</a>
      <br />
    </p>
  </td>
</tr>", FormatDatum(datum), filename, voorganger);
            }
        }

        private string FormatDatum(string jjjjmmdd)
        {
            string jaar = jjjjmmdd.Substring(0, 4);
            string maand = jjjjmmdd.Substring(4, 2);
            string dag = jjjjmmdd.Substring(6, 2);
            return string.Format("{0} {1} {2}", dag, maand, jaar);
        }
    }
}
