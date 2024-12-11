using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AUV_UI
{
    public partial class TextBoxMonitor : Form
    {
        public string _dosya_yolu, tag;
        public AnaForm Ana;

        public TextBoxMonitor(AnaForm ana, string yol, string etiket)
        {
            InitializeComponent();
            Ana = ana;
            _dosya_yolu = yol;
            tag = etiket;
            textBox1.Text = _dosya_yolu;
        }        

        private void Onaylama_Click(object sender, EventArgs e)
        {
            _dosya_yolu = textBox1.Text;
            Ana.bilgi_geri_al(_dosya_yolu, tag);
            this.Close();
        }
    }
}
