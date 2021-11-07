using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using System.IO;

namespace TadahLauncher
{
    public partial class Form1 : Form
    {
        string baseUrl = "http://tadah.rocks";
        string version = "1.0.FUCKCORPORATIONS";
        string path = Path.GetDirectoryName(Application.ExecutablePath);
        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateClient()
        {
            /*if (File.Exists(@path + @"\TadahUpdater.exe"))
            {
                Process.Start(@path + @"\TadahUpdater.exe");
                Close();
            }
            else
            {
                MessageBox.Show("Your install of Tadah is broken. Please reinstall.", "Broken Install", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }*/

            MessageBox.Show("Your version of Tadah is outdated or corrupted. Please update!", "Outdated Tadah", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
          

            if (args.Length < 2)
            {
                MessageBox.Show("Invalid arguments.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            if (args[1] != "-token")
            {
                MessageBox.Show("Invalid arguments.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            if (!File.Exists(@path + @"\TadahApp.exe"))
            {
                MessageBox.Show("The client couldn't be found, so we are going to install it.\nCurrent Directory: " + path, "Client Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateClient();
            }

            //MessageBox.Show("Token: " + args[2], "Supplied Token", MessageBoxButtons.OK, MessageBoxIcon.Information);

            label1.Text = "Checking version...";
            progressBar1.Value = 50;

            string receivedVersionString = "N/A";
            try
            {
                receivedVersionString = new WebClient().DownloadString(baseUrl + "/client/versionstring");
            }
            catch
            {
                label1.Text = "Error: Can't connect.";
                MessageBox.Show("Could not connect to Tadah.\n\nCurrent baseUrl: " + baseUrl + "\nLauncher version: " + version + "\n\nIf this persists, please send this message to the developers.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }
            
            if (receivedVersionString == version)
            {
                label1.Text = "Launching Tadah " + version + "...";
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = path + "\\TadahApp.exe",
                        Arguments = "-script dofile('" + baseUrl + "/client/join/" + args[2].Split(':')[1] + "')"
                    }
                };
                process.Start();
                progressBar1.Value = 100;
                await Task.Delay(5000);
                Close();
                return;
            }
            else
            {
                UpdateClient();
                return;
            }
        }
    }
}
