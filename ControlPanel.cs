using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AUV_UI
{
    public partial class ControlPanel : Form
    {
        public AnaForm Ana;
        public ControlPanel(AnaForm ana)
        {
            InitializeComponent();
            Ana = ana;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar6_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void Zero_Click(object sender, EventArgs e)
        {
            trackBar1.Value = 1500;
            trackBar2.Value = 1500;
            trackBar3.Value = 1500;
            trackBar4.Value = 1500;
            trackBar5.Value = 1500;
            trackBar6.Value = 1500;
            trackBar7.Value = 1500;
            trackBar8.Value = 1500;

            degeratama();
        }

        public void degeratama()
        {
            M1Value.Text = Convert.ToString(trackBar1.Value);
            M2Value.Text = Convert.ToString(trackBar2.Value);
            M3Value.Text = Convert.ToString(trackBar3.Value);
            M4Value.Text = Convert.ToString(trackBar4.Value);
            M5Value.Text = Convert.ToString(trackBar5.Value);
            M6Value.Text = Convert.ToString(trackBar6.Value);
            M7Value.Text = Convert.ToString(trackBar7.Value);
            M8Value.Text = Convert.ToString(trackBar8.Value);
        }

        public void degeratama_yenileme()
        {
            degeratama();
            MTP_islemi_gerceklestir();
        }

        private void MTP_islemi_gerceklestir()
        {
            try
            {
                byte[] TestMotorRTSayisal = Encoding.UTF8.GetBytes(Properties.Resources.TestMotorSayisalRT);
                using (var memoryStream = new MemoryStream(TestMotorRTSayisal))
                {
                    Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.Combine(Ana.raspi_dosya_yolu_Gonder, "TestMotorSayisalRT.py"));
                }
            }
            catch(Exception ex){ MessageBox.Show(ex.Message); }            
        }

        private void MTPB_Click(object sender, EventArgs e)
        {
            if (MTPB.Tag.ToString() == "T")
            {
                Zero_Click(sender, e);
                MTPB.Text = "Motor Test Panelini Durdur";
                MTPB.Tag = "F";
                MTPB.BackColor = Color.Blue;
                try
                {
                    byte[] TestMotorRTbyte = Properties.Resources.TestMotorRT;
                    using (var memoryStream = new MemoryStream(TestMotorRTbyte))
                    {
                        Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.Combine(Ana.raspi_dosya_yolu_Gonder, "TestMotorRT.py"));
                        Ana.terminal.Text += "TestMotorRT.py" + " dosyası;" + Environment.NewLine + Ana.raspi_dosya_yolu_Gonder + " adresine yüklendi" + Environment.NewLine;
                    }                    

                    byte[] TestMotorSayisalRTbyte = Encoding.UTF8.GetBytes(Properties.Resources.TestMotorSayisalRT);
                    using (var memoryStream = new MemoryStream(TestMotorSayisalRTbyte))
                    {
                        Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.Combine(Ana.raspi_dosya_yolu_Gonder, "TestMotorSayisalRT.py"));
                        Ana.terminal.Text += "TestMotorSayisalRT.py" + " dosyası;" + Environment.NewLine + Ana.raspi_dosya_yolu_Gonder + " adresine yüklendi" + Environment.NewLine;
                    }

                    Ana.shellStream.WriteLine("python Desktop/TestMotorRT.py");
                    string output;
                    Match match;

                    while (true)
                    {
                        output = Ana.shellStream.ReadLine() + "\n";
                        match = Regex.Match(output, @"%(\d+)%");
                        if (match.Success)
                        {
                            Ana.terminal.AppendText("islem kodu" + match.Groups[1].Value + Environment.NewLine);
                            Ana.currentProcessId = match.Groups[1].Value;
                            break;
                        }
                    }
                }
                catch(Exception ex) { MessageBox.Show(ex.Message); }
            }
            else if (MTPB.Tag.ToString() == "F")
            {
                MTPB.Text = "Motor Test Panelini Başlat";
                MTPB.Tag = "T";
                MTPB.BackColor = Color.Transparent;
                Zero_Click(sender, e);
                try
                {
                    Ana.RaspiSSHClient.CreateCommand($"kill -9 {Ana.currentProcessId}").Execute();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void trackBar8_Scroll(object sender, EventArgs e)
        {

        }

        private void trackBar16_Scroll(object sender, EventArgs e)
        {

        }

        private void trackBar15_Scroll(object sender, EventArgs e)
        {

        }

        private void trackBar14_Scroll(object sender, EventArgs e)
        {

        }

        private void trackBar13_Scroll(object sender, EventArgs e)
        {

        }

        private void telemetry_Click(object sender, EventArgs e)
        {

        }
    }
}
