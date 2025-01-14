using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        public string[] sonuc;
        public ShellStream TelemetriShell = null;
        private CancellationTokenSource _cancellationTokenSource;

        public string Telemstring = "None,None,None,None,None,None,None,None,None,None,None,None";
        private float?[] TelemTemp = new float?[12] {null, null , null , null , null , null , null , null , null , null , null , null };

        private int currentX1, currentX2, currentX3, currentX4;
        public ControlPanel(AnaForm ana)
        {
            InitializeComponent();
            this.Ana = ana;
            traklar = new TrackBar[12] { trackBar1, trackBar2, trackBar3, trackBar4, trackBar5, trackBar6, trackBar7, trackBar8, trackBar9, trackBar10, trackBar11, trackBar12 };
            trakboxlar = new TextBox[12] { M1Value, M2Value, M3Value, M4Value, M5Value, M6Value, M7Value, M8Value, M9Value, M10Value, M11Value, M12Value };
            
            InitializeChart(sicaklik, "Raspberry Pi °C", "STM32 °C", "MPU9250 °C", "Zaman", "Celcius °C", "Sıcaklık", ref currentX1);
            InitializeChart(Accel,  "Ax", "Ay", "Az", "Zaman", "m/s^2", "İvmeölçer", ref currentX2);
            InitializeChart(Gyro, "Gx", "Gy", "Gz", "Zaman", "°Degree/s", "Jiroskop", ref currentX3);
            InitializeChart(Pusula, "Cx", "Cy", "Cz", "Zaman", "uT (Tesla)", "Pusula", ref currentX4);

            if (Ana.Baglanti == true) { MTPB.Enabled = true; }
            else { MTPB.Enabled = false; }
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
                TelemetriShell = Ana.RaspiSSHClient.CreateShellStream("xterm", 80, 24, 800, 600, 4096);
                _cancellationTokenSource = new CancellationTokenSource();

                await Task.Delay(10);

                byte[] TestMotorSayisalRTbyte = Encoding.UTF8.GetBytes(Properties.Resources.TestMotorSayisalRT);
                using (var memoryStream = new MemoryStream(TestMotorSayisalRTbyte))
                {
                    Ana.RaspiSFTPClient.ChangeDirectory(Ana.raspi_dosya_yolu_Gonder);
                    Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.GetFileName("TestMotorSayisalRT.txt"));
                    Ana.terminal.Text += "TestMotorSayisalRT.txt" + " dosyası;" + Environment.NewLine + Ana.raspi_dosya_yolu_Gonder + " adresine yüklendi" + Environment.NewLine;
                }

                string telemetriText = Encoding.UTF8.GetString(Properties.Resources.Telem).Replace("Belirsiz_Adres", Ana.raspi_dosya_yolu_Gonder);
                byte[] Telemetri = Encoding.UTF8.GetBytes(telemetriText);
                using (var memoryStream = new MemoryStream(Telemetri))
                {
                    Ana.RaspiSFTPClient.ChangeDirectory(Ana.raspi_dosya_yolu_Gonder);
                    Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.GetFileName("Telem.py"));
                    Ana.terminal.Text += "Telem.py" + " dosyası;" + Environment.NewLine + Ana.raspi_dosya_yolu_Gonder + " adresine yüklendi" + Environment.NewLine;
                }                                

                TelemetriShell.WriteLine("python " + Ana.raspi_dosya_yolu_Gonder + "/Telem.py & echo C$!C; wait $!");

                string outputa;
                await Task.Run(() =>
                {
                    while (true)
                    {
                        outputa = TelemetriShell.ReadLine();
                        if (!string.IsNullOrEmpty(outputa))
                        {
                            Match match;
                            match = Regex.Match(outputa, @"C(\d+)C");
                            if (match.Success)
                            {
                                Invoke(new Action(() =>
                                {
                                    Ana.terminal.AppendText(Environment.NewLine + "islem kodu " + match.Groups[1].Value + Environment.NewLine);
                                    Ana.currentProcessId = match.Groups[1].Value;
                                    Ana.islemi_Durdur.Enabled = true;
                                    Ana.komutu_calistir.Enabled = false;
                                    Ana.ControlPanelButton.Enabled = false;
                                    Ana.label5.Text = "İşlem ID: " + match.Groups[1].Value;
                                }));
                                break;
                            }
                        }
                    }
                });

                Ana.label5.Text = "İşlem ID: " + Ana.currentProcessId.ToString();

                TelemetriVeriAl(_cancellationTokenSource.Token);
                TelemetriVeriGonder(_cancellationTokenSource.Token);                
                ChartDegerGuncelleme(_cancellationTokenSource.Token);
                TelemetriVeriAlisle(_cancellationTokenSource.Token);

            }
            catch (Exception ex) { MessageBox.Show(ex.Message); sistemi_durdur(null, null); }
        }

        private void sistemi_durdur(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            MTPB.Text = "Motor Test Panelini Başlat";
            MTPB.Tag = "T";
            MTPB.BackColor = Color.Transparent;
            Zero_Click(sender, e);
            try
            {
                var durdurmaislemi = Ana.RaspiSSHClient.CreateCommand($"sudo kill -9 {Ana.currentProcessId}");
                Ana.terminal.AppendText(durdurmaislemi.Execute());
                TelemetriShell.Dispose();                
                Ana.islemi_Durdur.Enabled = false;
                Ana.komutu_calistir.Enabled = true;
                Ana.ControlPanelButton.Enabled = true;
                TelemetriShell = null;
                Ana.label5.Text = "İşlem ID: " ;
                Ana.currentProcessId = null;
            }
            catch(Exception ex){ MessageBox.Show(ex.Message); }
        }

        private async Task TelemetriVeriAl(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Telemstring = await Task.Run(() => TelemetriShell.ReadLine());                    
                    Invoke(new Action(() => { Ana.terminal.Text = string.Join(" - ", sonuc.Skip(0).Take(12).ToArray()) + Environment.NewLine + string.Join(" - ", sonuc.Skip(12).Take(sonuc.Length-12).ToArray()); }));
                }                
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); sistemi_durdur(null, null); }
        }

        private async Task TelemetriVeriAlisle(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    sonuc = Telemstring.Split(',');
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
                    await Task.Delay(1);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); sistemi_durdur(null, null); }

        }

        private async Task TelemetriVeriGonder(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
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
                        await Task.Run(() => Ana.RaspiSFTPClient.UploadFile(memoryStream, Path.GetFileName("TestMotorSayisalRT.txt")));
                    }
                    await Task.Delay(1);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); sistemi_durdur(null, null); }
        }

        private void ControlPanel_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MTPB.Tag.ToString() == "F")
            {
                sistemi_durdur(sender, e);
            }
        }

        private void ChartIslem(Chart grafik, float? a, float? b, float? c, ref int current)
        {
            if (a != null) { grafik.Series[0].Points.AddXY(current, a); }
            if (b != null) { grafik.Series[1].Points.AddXY(current, b); }
            if (c != null) { grafik.Series[2].Points.AddXY(current, c); }
            current += 1;
            grafik.ChartAreas[0].AxisY.Maximum = Double.NaN;
            grafik.ChartAreas[0].AxisY.Minimum = Double.NaN;
            grafik.ChartAreas[0].RecalculateAxesScale();
            if (current <= 100)
            {
                grafik.ChartAreas[0].AxisX.Minimum = 0;
                grafik.ChartAreas[0].AxisX.Maximum = 100;
                grafik.ChartAreas[0].AxisX.CustomLabels.Clear();
                for (int i = 0; i <= 100; i += 10)
                {
                    grafik.ChartAreas[0].AxisX.CustomLabels.Add(i - 0.5, i + 0.5, (100 - i).ToString());
                }
            }
            else
            {
                grafik.ChartAreas[0].AxisX.Minimum = current - 100;
                grafik.ChartAreas[0].AxisX.Maximum = current;
                grafik.ChartAreas[0].AxisX.CustomLabels.Clear();
                for (int i = 0; i <= 100; i += 10)
                {
                    double labelPosition = current - 100 + i;
                    grafik.ChartAreas[0].AxisX.CustomLabels.Add(labelPosition - 0.5, labelPosition + 0.5, (100 - i).ToString());
                }
            }

            for (int i = 0; i < grafik.Series.Count; i++)
            {
                while (grafik.Series[i].Points.Count > 0 &&
                       grafik.Series[i].Points[0].XValue < current - 100)
                {
                    grafik.Series[i].Points.RemoveAt(0);
                }
            }
        }

        private async Task ChartDegerGuncelleme(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Invoke(new Action(() =>
                {
                    ChartIslem(sicaklik, TelemTemp[9], TelemTemp[10], TelemTemp[11], ref currentX1);
                    ChartIslem(Accel, TelemTemp[0], TelemTemp[1], TelemTemp[2], ref currentX2);
                    ChartIslem(Gyro, TelemTemp[3], TelemTemp[4], TelemTemp[5], ref currentX3);
                    ChartIslem(Pusula, TelemTemp[6], TelemTemp[7], TelemTemp[8], ref currentX4);
                }));
                await Task.Delay(10);
            }
        }

        private void InitializeChart(Chart grafik, string a, string b, string c, string d, string e, string f, ref int Current)
        {
            grafik.Series.Clear();

            Series series1 = new Series(a);
            series1.ChartType = SeriesChartType.Line;
            series1.BorderWidth = 2;
            series1.Color = System.Drawing.Color.Red;
            grafik.Series.Add(series1);

            Series series2 = new Series(b);
            series2.ChartType = SeriesChartType.Line;
            series2.BorderWidth = 2;
            series2.Color = System.Drawing.Color.Blue;
            grafik.Series.Add(series2);

            Series series3 = new Series(c);
            series3.ChartType = SeriesChartType.Line;
            series3.BorderWidth = 2;
            series3.Color = System.Drawing.Color.Green;
            grafik.Series.Add(series3);

            grafik.ChartAreas[0].AxisX.Title = d;
            grafik.ChartAreas[0].AxisY.Title = e;

            grafik.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            grafik.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            grafik.ChartAreas[0].AxisX.Interval = 10;
            grafik.ChartAreas[0].AxisX.Minimum = 0;
            grafik.ChartAreas[0].AxisX.Maximum = 100;
            grafik.Titles.Clear();
            grafik.Titles.Add(f);
            Current = 0;
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
    }
}
