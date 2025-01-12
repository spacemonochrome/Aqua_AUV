﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Renci.SshNet;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AUV_UI
{
    public partial class AnaForm : Form
    {
        public string host;
        public string username;
        public string password;
        public string command;

        public SshClient RaspiSSHClient = null;
        public SftpClient RaspiSFTPClient = null;
        public ShellStream shellStream = null;
        
        public string currentProcessId;

        public string raspi_dosya_yolu_Gonder;
        public string raspi_dosya_yolu_Al;
        public TextBoxMonitor yol_degis;
        public MotorConfigurationForm Konfigurasyon_Form;
        public ControlPanel kontrol_formu;
        public bool devamlilik_Sensor = false;
        public bool Baglanti = false;


        private string currentPath = "/home/pi/Desktop"; // Başlangıç dizini
        public string SelectedFilePath { get; private set; } // Seçilen dosya yolu

        public AnaForm()
        {
            InitializeComponent();

            ipadress_label.Text = Properties.Settings.Default.IP;
            password_label.Text = Properties.Settings.Default.Password;
            username_label.Text = Properties.Settings.Default.UserName;
            komut_satiri.Text = Properties.Settings.Default.KomutSatiri;
        }

        public void bilgi_geri_al(string _dosya_yolu, string tag)
        {
            if (tag == "al")
            {
                raspi_dosya_yolu_Al = _dosya_yolu;
            }

            else if (tag == "gonder")
            {
                raspi_dosya_yolu_Gonder = _dosya_yolu;
            }
        }

        private void baglan_Click(object sender, EventArgs e)
        {
            try
            {
                host = ipadress_label.Text;
                username = username_label.Text;
                password = password_label.Text;

                RaspiSSHClient = new SshClient(host, username, password);
                RaspiSFTPClient = new SftpClient(host, username, password);

                RaspiSSHClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(1);
                RaspiSFTPClient.ConnectionInfo.Timeout = TimeSpan.FromSeconds(1);

                RaspiSSHClient.Connect();
                RaspiSFTPClient.Connect();

                if (RaspiSSHClient.IsConnected && RaspiSSHClient.IsConnected)
                {
                    shellStream = RaspiSSHClient.CreateShellStream("xterm", 80, 24, 800, 600, 1024);
                    Baglanti = true;
                    var cmd = RaspiSSHClient.CreateCommand("echo -n $HOME/Desktop");
                    var result = cmd.Execute();
                    raspi_dosya_yolu_Gonder = result;
                    raspi_dosya_yolu_Al = result;
                    pictureBox2.Image = Properties.Resources.greentick;
                    desktop_listesi.Enabled = true;
                    dosya_gonder.Enabled = true;
                    baglantiyi_kes.Enabled = true;
                    komutu_calistir.Enabled = true;
                    alincak.Enabled = true;
                    baglan.Enabled = false;
                    Dosya_Yolu_Degistir.Enabled = true;

                    try{shell_Baglan();}
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                }
                else
                {
                    Baglanti = false;
                    pictureBox2.Image = Properties.Resources.rederror;
                    desktop_listesi.Enabled = false;
                    dosya_gonder.Enabled = false;
                    baglantiyi_kes.Enabled = false;
                    Dosya_Yolu_Degistir.Enabled = false;
                    MessageBox.Show("Bağlanamadı! \n");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
                    
        }

        private async void shell_Baglan()
        {
            string output;
            await Task.Run(() =>
            {
                while (RaspiSSHClient != null)
                {
                    output = shellStream.ReadLine() + "\n";
                    if (!string.IsNullOrEmpty(output))
                    {
                        Invoke(new Action(() =>
                        {
                            Match match;
                            match = Regex.Match(output, @"C(\d+)C");
                            if (match.Success)
                            {
                                terminal.AppendText(Environment.NewLine + "islem kodu " + match.Groups[1].Value + Environment.NewLine);
                                currentProcessId = match.Groups[1].Value;
                                islemi_Durdur.Enabled = true;
                                komutu_calistir.Enabled = false;
                                ControlPanelButton.Enabled = false;
                                label5.Text = "İşlem ID: " + currentProcessId;
                            }

                            int startIndex = output.IndexOf("Done");
                            if (startIndex != -1)
                            {
                                islemi_Durdur.Enabled = false;
                                komutu_calistir.Enabled = true;
                                ControlPanelButton.Enabled = true;
                                terminal.AppendText(Environment.NewLine + "İşlem Bitti" + Environment.NewLine);
                                label5.Text = "İşlem ID: ";
                            }

                            terminal.AppendText(output + Environment.NewLine);

                        }));
                    }
                }
            });            
        }

        private void baglantiyi_kes_Click(object sender, EventArgs e)
        {
            try
            {
                shellStream.Close();
                RaspiSSHClient.Disconnect();
                RaspiSFTPClient.Disconnect();
                pictureBox2.Image = Properties.Resources.rederror;
                RaspiSSHClient = null;
                RaspiSFTPClient = null;
                shellStream = null;

                desktop_listesi.Enabled = false;
                dosya_gonder.Enabled = false;
                baglantiyi_kes.Enabled = false;
                komutu_calistir.Enabled = false;
                alincak.Enabled = false;
                baglan.Enabled = true;
                Dosya_Yolu_Degistir.Enabled = false;
            }
            catch(Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void desktop_listesi_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                folderDialog.Description = "Bir klasör seçin";
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    MessageBox.Show("Seçilen yol: " + selectedPath);
                    using (var file = File.Create(selectedPath + "/" + Path.GetFileName(raspi_dosya_yolu_Al)))
                    {
                        RaspiSFTPClient.DownloadFile(raspi_dosya_yolu_Al, file);
                        terminal.AppendText(Environment.NewLine + raspi_dosya_yolu_Al + Environment.NewLine + "Adresinden dosya başarı ile alındı ve " + Environment.NewLine + selectedPath + Path.GetFileName(raspi_dosya_yolu_Al) + Environment.NewLine + "Adresine kaydedildi " + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void alincak_Click(object sender, EventArgs e)
        {
            yol_degis = new TextBoxMonitor(this, raspi_dosya_yolu_Al, "al");
            yol_degis.ShowDialog();
        }

        private void dosya_gonder_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = true;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dialog.RestoreDirectory = true;
                dialog.Multiselect = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (String file in dialog.FileNames)
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open))
                        {
                            RaspiSFTPClient.ChangeDirectory(raspi_dosya_yolu_Gonder);
                            RaspiSFTPClient.UploadFile(fs, Path.GetFileName(file));
                            terminal.Text += file + " dosyası;" + Environment.NewLine + raspi_dosya_yolu_Gonder + " adresine yüklendi" + Environment.NewLine;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex + "");
            }
        }

        private void Dosya_Yolu_Degistir_Click(object sender, EventArgs e)
        {
            yol_degis = new TextBoxMonitor(this, raspi_dosya_yolu_Gonder, "gonder");
            yol_degis.ShowDialog();
        }

        private void config_button_Click(object sender, EventArgs e)
        {
            Konfigurasyon_Form = new MotorConfigurationForm(this);
            Konfigurasyon_Form.ShowDialog();
        }

        private void komutu_calistir_Click(object sender, EventArgs e)
        {
            try
            {
                ControlPanelButton.Enabled = false;
                shellStream.WriteLine(komut_satiri.Text + " & echo C$!C; wait $!");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }            
        }

        public void islemi_Durdur_Click(object sender, EventArgs e)
        {
            try
            {
                var durdurmaislemi = RaspiSSHClient.CreateCommand($"sudo kill -9 {currentProcessId}");
                terminal.Text += durdurmaislemi.Execute();
                islemi_Durdur.Enabled = false;
                komutu_calistir.Enabled = true;
                ControlPanelButton.Enabled = true;
                currentProcessId = null;
                label5.Text = "İşlem ID: ";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }            
        }

        private void AnaForm_Closing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.IP = ipadress_label.Text;
            Properties.Settings.Default.Password = password_label.Text;
            Properties.Settings.Default.UserName = username_label.Text;
            Properties.Settings.Default.KomutSatiri = komut_satiri.Text;
            Properties.Settings.Default.Save();
        }

        private void ControlPanelButton_Click(object sender, EventArgs e)
        {
            kontrol_formu = new ControlPanel(this);
            kontrol_formu.ShowDialog();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            string url = "https://github.com/spacemonochrome/Aqua_AUV";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlantı açılamadı: " + ex.Message);
            }
        }
       
        private void button1_Click(object sender, EventArgs e)
        {
            
        }        

        private void button2_Click(object sender, EventArgs e)
        {
            
        }
    }
}
