using LiveCharts.Wpf.Charts.Base;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AUV_UI
{
    public partial class ControlPanel : Form
    {
        public AnaForm Ana;
        public string gidenValue;
        public TrackBar[] traklar;
        public TextBox[] trakboxlar;
        public System.Windows.Forms.Timer timer1;
        public bool devamlilik_Sensor = false;
        public string Telemstring;
        private float[] TelemTemp = new float[12] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ,0, 0 };

        private System.Windows.Forms.Timer timer2; // Zamanlayıcı
        private int currentX; // X ekseni değerini takip eden sayaç
        public ControlPanel(AnaForm ana)
        {
            InitializeComponent();
            Ana = ana;
            traklar = new TrackBar[12] { trackBar1, trackBar2, trackBar3, trackBar4, trackBar5, trackBar6, trackBar7, trackBar8, trackBar9, trackBar10, trackBar11, trackBar12 };
            trakboxlar = new TextBox[12] { M1Value, M2Value, M3Value, M4Value, M5Value, M6Value, M7Value, M8Value, M9Value, M10Value, M11Value, M12Value };
            InitializeChartTemp();
            InitializeChartGyro();
            InitializeChartAccel();
            InitializeChartCompass();

            if (Ana.Baglanti == true) { MTPB.Enabled = true; }
            else { MTPB.Enabled = false; }
        }
        private void trackBar12_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar11_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar10_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }

        private void trackBar9_Scroll(object sender, EventArgs e)
        {
            degeratama();
        }


        private void trackBar8_Scroll(object sender, EventArgs e)
        {
            degeratama();
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
            for (int i = 0; i < traklar.Length; i++)
            {
                traklar[i].Value = 1500;
            }
            degeratama();
        }

        public void degeratama()
        {
            for (int i = 0; i < traklar.Length; i++)
            {
                trakboxlar[i].Text = Convert.ToString(traklar[i].Value);
            }
        }

        private void MTP_islemi_gerceklestir()
        {
            try
            {
                gidenValue = "";
                for (int i = 0; i < traklar.Length; i++)
                {
                    int trackBarValue = 0;
                    if (traklar[i].InvokeRequired)
                    {
                        trackBarValue = (int)traklar[i].Invoke(new Func<int>(() => traklar[i].Value));
                    }
                    else
                    {
                        trackBarValue = traklar[i].Value;
                    }
                    gidenValue = gidenValue + Convert.ToInt32(((trackBarValue - 1000)/5) +25) + ",";
                }
                byte[] TestMotorSayisalRTbyte = Encoding.UTF8.GetBytes(gidenValue);
                using (var memoryStream = new MemoryStream(TestMotorSayisalRTbyte))
                {
                    Ana.RaspiSFTPClient.ChangeDirectory(Ana.raspi_dosya_yolu_Gonder);
                    Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.GetFileName("TestMotorSayisalRT.txt"));
                }

                using (var memoryStream = new MemoryStream())
                {
                    Ana.RaspiSFTPClient.DownloadFile(Ana.raspi_dosya_yolu_Gonder + "/telemetri_verisi.txt", memoryStream);

                    // Belleğe indirilen dosya içeriğini okuma
                    memoryStream.Seek(0, SeekOrigin.Begin); // Bellek akışını başa sarıyoruz
                    using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                    {
                        string a = reader.ReadToEnd();
                        if (this.InvokeRequired && !string.IsNullOrEmpty(a))
                        {
                            this.Invoke(new Action(() =>
                            {
                                string[] sonuc = a.Split(',');
                                for (int i = 0; i < TelemTemp.Length; i++)
                                {
                                    if (float.TryParse(sonuc[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                                    {
                                        TelemTemp[i] = floatValue;
                                    }
                                    else
                                    {
                                        TelemTemp[i] = 0;
                                    }
                                }
                                Telemstring = a;
                            }));
                        }
                    }
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
                    byte[] TestMotorSayisalRTbyte = Encoding.UTF8.GetBytes(Properties.Resources.TestMotorSayisalRT);
                    using (var memoryStream = new MemoryStream(TestMotorSayisalRTbyte))
                    {
                        Ana.RaspiSFTPClient.ChangeDirectory(Ana.raspi_dosya_yolu_Gonder);
                        Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.GetFileName("TestMotorSayisalRT.txt"));
                        Ana.terminal.Text += "TestMotorSayisalRT.txt" + " dosyası;" + Environment.NewLine + Ana.raspi_dosya_yolu_Gonder + " adresine yüklendi" + Environment.NewLine;
                    }
                    string Telemetri = Properties.Resources.Telem.Replace("Belirsiz_Adres", Ana.raspi_dosya_yolu_Gonder);
                    byte[] telem = Encoding.UTF8.GetBytes(Telemetri);
                    using (var memoryStream = new MemoryStream(telem))
                    {
                        Ana.RaspiSFTPClient.ChangeDirectory(Ana.raspi_dosya_yolu_Gonder);
                        Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.GetFileName("Telem.py"));
                        Ana.terminal.Text += "Telem.py" + " dosyası;" + Environment.NewLine + Ana.raspi_dosya_yolu_Gonder + " adresine yüklendi" + Environment.NewLine;
                    }
                    string output;
                    Match match;

                    string guzergah = "python " + Ana.raspi_dosya_yolu_Gonder + "/Telem.py & echo C$!C; wait $!";
                    Ana.shellStream.WriteLine(guzergah);
                    output = Ana.shellStream.ReadLine() + "\n";
                    if (output != null || output != "")
                    {
                        match = Regex.Match(output, @"C(\d+)C");
                        if (match.Success)
                        {
                            Ana.terminal.AppendText("islem kodu" + match.Groups[1].Value + Environment.NewLine);
                            Ana.currentProcessId = match.Groups[1].Value;
                        }
                    }
                    InitializeTimer1();                    
                    InitializeTimer2();
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
                    timer1.Stop();
                    timer2.Stop();
                    Ana.islemi_Durdur_Click(sender, e);
                    //Ana.RaspiSSHClient.CreateCommand($"kill -9 {Ana.currentProcessId}").Execute();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void InitializeTimer1()
        {
            timer1 = new System.Windows.Forms.Timer();
            timer1.Interval = 50; // 5 ms
            timer1.Tick += async (sender, e) => await Timer1TickAsync();
            timer1.Start();
        }

        private async Task Timer1TickAsync()
        {
            await Task.Run(() =>
            {
                MTP_islemi_gerceklestir();
            });
        }

        private void InitializeTimer2()
        {
            // Zamanlayıcı ayarları
            timer2 = new System.Windows.Forms.Timer();
            timer2.Interval = 100;
            timer2.Tick += Timer_Tick2;
            timer2.Start();
        }        

        private void Timer_Tick2(object sender, EventArgs e)
        {

            if (TelemTemp[9] > 0) { sicaklik.Series[0].Points.AddXY(currentX, TelemTemp[9]); }
            if (TelemTemp[10] > 0) { sicaklik.Series[0].Points.AddXY(currentX, TelemTemp[10]); }
            if (TelemTemp[11] > 0) { sicaklik.Series[0].Points.AddXY(currentX, TelemTemp[11]); }
            currentX += 1;

            sicaklik.ChartAreas[0].AxisY.Maximum = Double.NaN;
            sicaklik.ChartAreas[0].AxisY.Minimum = Double.NaN;
            sicaklik.ChartAreas[0].RecalculateAxesScale();

            sicaklik.ChartAreas[0].AxisX.Minimum = Math.Max(0, currentX - 30); // Başlangıç
            sicaklik.ChartAreas[0].AxisX.Maximum = Math.Max(30, currentX);     // Bitiş


            for (int i = 0; i < sicaklik.Series.Count; i++)
            {
                while (sicaklik.Series[i].Points.Count > 0 &&
                       sicaklik.Series[i].Points[0].XValue < currentX - 30)
                {
                    sicaklik.Series[i].Points.RemoveAt(0);
                }
            }
        }

        private void InitializeChartTemp()
        {
            sicaklik.Series.Clear();

            Series series1 = new Series("Pi °C");
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            sicaklik.Series.Add(series1);

            Series series2 = new Series("MPU °C");
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            sicaklik.Series.Add(series2);

            Series series3 = new Series("STM °C");
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            sicaklik.Series.Add(series3);

            sicaklik.ChartAreas[0].AxisX.Title = "Zaman (saniye)";
            sicaklik.ChartAreas[0].AxisY.Title = "Celcius";

            sicaklik.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            sicaklik.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            sicaklik.ChartAreas[0].AxisX.Interval = 10;
            sicaklik.ChartAreas[0].AxisX.Minimum = 0;
            sicaklik.ChartAreas[0].AxisX.Maximum = 30;
            sicaklik.Titles.Clear();
            sicaklik.Titles.Add("Pi MPU STM °C");
            currentX = 0;
        }

        private void InitializeChartGyro()
        {
            Gyro.Series.Clear();

            Series series1 = new Series("Gx");
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            Gyro.Series.Add(series1);

            Series series2 = new Series("Gy");
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            Gyro.Series.Add(series2);

            Series series3 = new Series("Gz");
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            Gyro.Series.Add(series3);

            Gyro.ChartAreas[0].AxisX.Title = "Zaman (saniye)";
            Gyro.ChartAreas[0].AxisY.Title = "m/s";

            Gyro.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            Gyro.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            Gyro.ChartAreas[0].AxisX.Interval = 10; // X ekseninde aralıklar 10 birim
            Gyro.ChartAreas[0].AxisX.Minimum = 0;  // X ekseninin başlangıcı
            Gyro.ChartAreas[0].AxisX.Maximum = 30; // X ekseninin sonu

            Gyro.Titles.Clear();
            Gyro.Titles.Add("Gyro m/s");

            currentX = 0;
        }

        private void InitializeChartAccel()
        {
            Accel.Series.Clear();

            Series series1 = new Series("Ax");
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            Accel.Series.Add(series1);

            Series series2 = new Series("Ay");
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            Accel.Series.Add(series2);

            Series series3 = new Series("Az");
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            Accel.Series.Add(series3);

            Accel.ChartAreas[0].AxisX.Title = "Zaman (saniye)";
            Accel.ChartAreas[0].AxisY.Title = "m/s";

            Accel.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            Accel.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            Accel.ChartAreas[0].AxisX.Interval = 10; // X ekseninde aralıklar 10 birim
            Accel.ChartAreas[0].AxisX.Minimum = 0;  // X ekseninin başlangıcı
            Accel.ChartAreas[0].AxisX.Maximum = 30; // X ekseninin sonu

            Accel.Titles.Clear();
            Accel.Titles.Add("Accel m/s");

            currentX = 0;
        }

        private void InitializeChartCompass()
        {
            Pusula.Series.Clear();

            Series series1 = new Series("Cx");
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            Pusula.Series.Add(series1);

            Series series2 = new Series("Cy");
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            Pusula.Series.Add(series2);

            Series series3 = new Series("Cz");
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            Pusula.Series.Add(series3);

            Pusula.ChartAreas[0].AxisX.Title = "Zaman (saniye)";
            Pusula.ChartAreas[0].AxisY.Title = "m/s";

            Pusula.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            Pusula.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            Pusula.ChartAreas[0].AxisX.Interval = 10; // X ekseninde aralıklar 10 birim
            Pusula.ChartAreas[0].AxisX.Minimum = 0;  // X ekseninin başlangıcı
            Pusula.ChartAreas[0].AxisX.Maximum = 30; // X ekseninin sonu

            Pusula.Titles.Clear();
            Pusula.Titles.Add("Pusula");

            currentX = 0;
        }

    }
}
