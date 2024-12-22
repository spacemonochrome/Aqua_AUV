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

        public ShellStream TelemetriShell = null;

        public bool devamlilik_Sensor = false;
        public string Telemstring;
        private float?[] TelemTemp = new float?[12] {null, null , null , null , null , null , null , null , null , null , null , null };

        private bool isRunning = false;

        public System.Windows.Forms.Timer timer1;
        private int currentX1, currentX2, currentX3, currentX4;
        public ControlPanel(AnaForm ana)
        {
            InitializeComponent();
            this.Ana = ana;
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

        private async void TelemetriVeriGonder()
        {
            try
            {
                while (isRunning)
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
                        gidenValue = gidenValue + Convert.ToInt32(((trackBarValue - 1000) / 5) + 25) + ",";
                    }
                    byte[] TestMotorSayisalRTbyte = Encoding.UTF8.GetBytes(gidenValue);
                    using (var memoryStream = new MemoryStream(TestMotorSayisalRTbyte))
                    {
                        Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.GetFileName("TestMotorSayisalRT.txt"));
                    }
                    await Task.Delay(10);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); sistemi_durdur(null, null); }
        }

        private void MTPB_Click(object sender, EventArgs e)
        {
            if (MTPB.Tag.ToString() == "T")
            {
                sistemi_calistir(sender, e);                
            }
            else if (MTPB.Tag.ToString() == "F")
            {
                sistemi_durdur(sender, e);
            }
        }
        private async void sistemi_calistir(object sender, EventArgs e)
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

                string guzergah = "python " + Ana.raspi_dosya_yolu_Gonder + "/Telem.py & echo C$!C; wait $!";
                Ana.shellStream.WriteLine(guzergah);

                isRunning = true;
                TelemetriShell = Ana.RaspiSSHClient.CreateShellStream("xterm", 80, 24, 800, 600, 4096);
                await Task.Delay(100);

                TelemetriShell.WriteLine("python Desktop/Telem.py & echo C$!C; wait $!");

                Tekilfonk();
                TelemetriVeriGonder();

            }
            catch (Exception ex) { MessageBox.Show(ex.Message); sistemi_durdur(null,null); }
        }

        private async void Tekilfonk()
        {
            try
            {
                string output;
                await Task.Run(() =>
                {
                    while (Ana.RaspiSSHClient != null)
                    {
                        output = TelemetriShell.ReadLine() + "\n";
                        if (!string.IsNullOrEmpty(output))
                        {
                            Match match;
                            match = Regex.Match(output, @"C(\d+)C");
                            if (match.Success)
                            {
                                Invoke(new Action(() =>
                                {
                                    Ana.terminal.AppendText(Environment.NewLine + " Telemetri akışı islem kodu " + match.Groups[1].Value + Environment.NewLine);
                                    Ana.currentProcessId = match.Groups[1].Value;
                                    Ana.islemi_Durdur.Enabled = true;
                                    Ana.komutu_calistir.Enabled = false;
                                    Ana.ControlPanelButton.Enabled = false;
                                }));
                                break;
                            }                                                        
                        }
                    }
                    while (Ana.RaspiSSHClient != null && TelemetriShell != null)
                    {
                        output = TelemetriShell.ReadLine() + "\n";
                        if (!string.IsNullOrEmpty(output))
                        {
                            if (true)
                            {
                                
                            }
                            Invoke(new Action(() =>
                            {
                                Ana.terminal.Text = output;
                            }));
                            string[] sonuc = output.Split(',');
                            for (int i = 0; i < TelemTemp.Length; i++)
                            {
                                if (float.TryParse(sonuc[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                                {
                                    TelemTemp[i] = floatValue;
                                }
                                else
                                {
                                    TelemTemp[i] = null;
                                }
                            }
                            Telemstring = output;

                            ChartDegerGuncelleme();
                        }
                    }                     
                        
                });
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void sistemi_durdur(object sender, EventArgs e)
        {
            MTPB.Text = "Motor Test Panelini Başlat";
            MTPB.Tag = "T";
            MTPB.BackColor = Color.Transparent;
            Zero_Click(sender, e);
            try
            {
                isRunning = false;                
                Ana.islemi_Durdur_Click(sender, e);
                TelemetriShell = null;
            }
            catch(Exception ex){ MessageBox.Show(ex.Message); }
        }
                
        private async void ChartDegerGuncelleme()
        {
            Invoke(new Action(() => {
                if (TelemTemp[9] != null) { sicaklik.Series[0].Points.AddXY(currentX1, TelemTemp[9]); }
            if (TelemTemp[10] != null) { sicaklik.Series[1].Points.AddXY(currentX1, TelemTemp[10]); }
            if (TelemTemp[11] != null) { sicaklik.Series[2].Points.AddXY(currentX1, TelemTemp[11]); }
            currentX1 += 1;

            sicaklik.ChartAreas[0].AxisY.Maximum = Double.NaN;
            sicaklik.ChartAreas[0].AxisY.Minimum = Double.NaN;
            sicaklik.ChartAreas[0].RecalculateAxesScale();

            sicaklik.ChartAreas[0].AxisX.Minimum = Math.Max(0, currentX1 - 30); // Başlangıç
            sicaklik.ChartAreas[0].AxisX.Maximum = Math.Max(30, currentX1);     // Bitiş


            for (int i = 0; i < sicaklik.Series.Count; i++)
            {
                while (sicaklik.Series[i].Points.Count > 0 &&
                       sicaklik.Series[i].Points[0].XValue < currentX1 - 30)
                {
                    sicaklik.Series[i].Points.RemoveAt(0);
                }
            }

            if (TelemTemp[0] != null) { Accel.Series[0].Points.AddXY(currentX2, TelemTemp[0]); }
            if (TelemTemp[1] != null) { Accel.Series[1].Points.AddXY(currentX2, TelemTemp[1]); }
            if (TelemTemp[2] != null) { Accel.Series[2].Points.AddXY(currentX2, TelemTemp[2]); }
            currentX2 += 1;

            Accel.ChartAreas[0].AxisY.Maximum = Double.NaN;
            Accel.ChartAreas[0].AxisY.Minimum = Double.NaN;
            Accel.ChartAreas[0].RecalculateAxesScale();

            Accel.ChartAreas[0].AxisX.Minimum = Math.Max(0, currentX2 - 30); // Başlangıç
            Accel.ChartAreas[0].AxisX.Maximum = Math.Max(30, currentX2);     // Bitiş


            for (int i = 0; i < sicaklik.Series.Count; i++)
            {
                while (Accel.Series[i].Points.Count > 0 &&
                       Accel.Series[i].Points[0].XValue < currentX2 - 30)
                {
                    Accel.Series[i].Points.RemoveAt(0);
                }
            }

            if (TelemTemp[3] != null) { Gyro.Series[0].Points.AddXY(currentX3, TelemTemp[3]); }
            if (TelemTemp[4] != null) { Gyro.Series[1].Points.AddXY(currentX3, TelemTemp[4]); }
            if (TelemTemp[5] != null) { Gyro.Series[2].Points.AddXY(currentX3, TelemTemp[5]); }
            currentX3 += 1;

            Gyro.ChartAreas[0].AxisY.Maximum = Double.NaN;
            Gyro.ChartAreas[0].AxisY.Minimum = Double.NaN;
            Gyro.ChartAreas[0].RecalculateAxesScale();

            Gyro.ChartAreas[0].AxisX.Minimum = Math.Max(0, currentX3 - 30); // Başlangıç
            Gyro.ChartAreas[0].AxisX.Maximum = Math.Max(30, currentX3);     // Bitiş


            for (int i = 0; i < Gyro.Series.Count; i++)
            {
                while (Gyro.Series[i].Points.Count > 0 &&
                       Gyro.Series[i].Points[0].XValue < currentX3 - 30)
                {
                    Gyro.Series[i].Points.RemoveAt(0);
                }
            }

            if (TelemTemp[6] != null) { Pusula.Series[0].Points.AddXY(currentX4, TelemTemp[6]); }
            if (TelemTemp[7] != null) { Pusula.Series[1].Points.AddXY(currentX4, TelemTemp[7]); }
            if (TelemTemp[8] != null) { Pusula.Series[2].Points.AddXY(currentX4, TelemTemp[8]); }
            currentX4 += 1;

            Pusula.ChartAreas[0].AxisY.Maximum = Double.NaN;
            Pusula.ChartAreas[0].AxisY.Minimum = Double.NaN;
            Pusula.ChartAreas[0].RecalculateAxesScale();

            Pusula.ChartAreas[0].AxisX.Minimum = Math.Max(0, currentX3 - 30); // Başlangıç
            Pusula.ChartAreas[0].AxisX.Maximum = Math.Max(30, currentX3);     // Bitiş
            
            for (int i = 0; i < Pusula.Series.Count; i++)
            {
                while (Pusula.Series[i].Points.Count > 0 &&
                       Pusula.Series[i].Points[0].XValue < currentX4 - 30)
                {
                    Pusula.Series[i].Points.RemoveAt(0);
                }
            }
            }));
        }
        private void InitializeChartTemp()
        {
            sicaklik.Series.Clear();

            Series series1 = new Series("Raspberry Pi °C");
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            series1.Color = System.Drawing.Color.Red;
            sicaklik.Series.Add(series1);

            Series series2 = new Series("STM32 °C");
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            series2.Color = System.Drawing.Color.Blue;
            sicaklik.Series.Add(series2);

            Series series3 = new Series("MPU9250 °C");
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            series3.Color = System.Drawing.Color.Green;
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
            currentX1 = 0;
        }

        private void InitializeChartGyro()
        {
            Gyro.Series.Clear();

            Series series1 = new Series("Gx");
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            series1.Color = System.Drawing.Color.Red;
            Gyro.Series.Add(series1);

            Series series2 = new Series("Gy");
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            series2.Color = System.Drawing.Color.Blue;
            Gyro.Series.Add(series2);

            Series series3 = new Series("Gz");
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            series3.Color = System.Drawing.Color.Green;
            Gyro.Series.Add(series3);

            Gyro.ChartAreas[0].AxisX.Title = "Zaman (saniye)";
            Gyro.ChartAreas[0].AxisY.Title = "°Derece/s";

            Gyro.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            Gyro.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            Gyro.ChartAreas[0].AxisX.Interval = 10; // X ekseninde aralıklar 10 birim
            Gyro.ChartAreas[0].AxisX.Minimum = 0;  // X ekseninin başlangıcı
            Gyro.ChartAreas[0].AxisX.Maximum = 30; // X ekseninin sonu

            Gyro.Titles.Clear();
            Gyro.Titles.Add("Gyro °Derece/s");

            currentX3 = 0;
        }

        private void InitializeChartAccel()
        {
            Accel.Series.Clear();

            Series series1 = new Series("Ax");
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            series1.Color = System.Drawing.Color.Red;
            Accel.Series.Add(series1);

            Series series2 = new Series("Ay");
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            series2.Color = System.Drawing.Color.Blue;
            Accel.Series.Add(series2);

            Series series3 = new Series("Az");
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            series3.Color = System.Drawing.Color.Green;
            Accel.Series.Add(series3);

            Accel.ChartAreas[0].AxisX.Title = "Zaman (saniye)";
            Accel.ChartAreas[0].AxisY.Title = "m/s";

            Accel.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            Accel.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            Accel.ChartAreas[0].AxisX.Interval = 10;
            Accel.ChartAreas[0].AxisX.Minimum = 0;
            Accel.ChartAreas[0].AxisX.Maximum = 30;

            Accel.Titles.Clear();
            Accel.Titles.Add("Accel m/s");

            currentX2 = 0;
        }

        private void InitializeChartCompass()
        {
            Pusula.Series.Clear();

            Series series1 = new Series("Cx");
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            series1.Color = System.Drawing.Color.Red;
            Pusula.Series.Add(series1);

            Series series2 = new Series("Cy");
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            series2.Color = System.Drawing.Color.Blue;
            Pusula.Series.Add(series2);

            Series series3 = new Series("Cz");
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            series3.Color = System.Drawing.Color.Green;
            Pusula.Series.Add(series3);

            Pusula.ChartAreas[0].AxisX.Title = "Zaman (saniye)";
            Pusula.ChartAreas[0].AxisY.Title = "uT";

            Pusula.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            Pusula.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            Pusula.ChartAreas[0].AxisX.Interval = 10; // X ekseninde aralıklar 10 birim
            Pusula.ChartAreas[0].AxisX.Minimum = 0;  // X ekseninin başlangıcı
            Pusula.ChartAreas[0].AxisX.Maximum = 30; // X ekseninin sonu

            Pusula.Titles.Clear();
            Pusula.Titles.Add("Pusula");

            currentX4 = 0;
        }

    }
}
