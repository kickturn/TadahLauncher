using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace TadahLauncher
{
    public partial class Form1 : Form
    {
        static string baseUrl = "http://tadah.rocks";
        static string version = "1.0.5";
        static string client = "2010";
        static string path = Path.GetDirectoryName(Application.ExecutablePath);
        static string installPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tadah\\" + client;

        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateClient(bool doInstall)
        {
            label1.Text = "Getting the latest Tadah...";

            string recievedClientData;
            try
            {
                recievedClientData = new WebClient().DownloadString(baseUrl + "/client/" + client);
            }
            catch
            {
                label1.Text = "Error: Can't connect.";
                MessageBox.Show("Could not connect to Tadah.\n\nCurrent baseUrl: " + baseUrl + "\nLauncher version: " + version + "\n\nIf this persists, please send this message to the developers.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            string versionString = "false";
            string downloadUrl = "false";
            string sha512 = "none";

            try
            {
                JObject clientData = JObject.Parse(recievedClientData);

                versionString = (string)clientData["version"];
                downloadUrl = (string)clientData["url"];
                sha512 = (string)clientData["sha512"];
            }
            catch
            {
                MessageBox.Show("Could not parse client info JSON data. This usually occurs if the webserver gives an invalid response. Contact the developers if this issue persists.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            if (sha512 == "none")
            {
                MessageBox.Show("The client exists on the webserver, but it is not downloadable anymore. If you believe this is an error, contact the developers. (sha512 is \"none\").", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            if (doInstall)
            {
                // Assuming we're in the temp folder or the user knows what they're doing, start replacing files.
                label1.Text = "Downloading latest client...";

                WebClient client = new WebClient();
                string tempZipArchivePath = Path.GetTempPath() + "Tadah" + client + ".zip";

                client.DownloadProgressChanged += (s, e) =>
                {
                    progressBar1.Value = e.ProgressPercentage;
                };

                client.DownloadFileCompleted += (s, e) =>
                {
                    label1.Text = "Verifying downloaded files...";

                    SHA512 cSha512 = SHA512.Create();

                    byte[] zipArchiveSha512Bytes;
                    using (FileStream stream = File.OpenRead(tempZipArchivePath))
                    {
                        zipArchiveSha512Bytes = cSha512.ComputeHash(stream);
                    }

                    string sha512result = "";
                    foreach (byte b in zipArchiveSha512Bytes) sha512result += b.ToString("x2");

                    if (sha512result != sha512)
                    {
                        MessageBox.Show("SHA512 mismatch.\nWebsite reported: " + sha512 + "\nLocal file: " + sha512result, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                        return;
                    }
                };

                try
                {
                    client.DownloadFileAsync(new Uri(baseUrl + "/client/download/" + client), tempZipArchivePath);
                }
                catch
                {
                    MessageBox.Show("Could not get latest client files from the website.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }
            }
            else
            {
                // Clone the installer to the temp folder so we can actually install.

                string currentExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string copiedExecutablePath = Path.GetTempPath() + "TadahLauncher" + client + ".exe";

                File.Copy(currentExecutablePath, copiedExecutablePath, true);

                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = copiedExecutablePath,
                        Arguments = "-update"
                    }
                };
                process.Start();

                Close();
                return;
            }
        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
          
            if (args.Length < 2)
            {
                UpdateClient(false);
                return;
            }

            if (args[1] == "-update")
            {
                UpdateClient(true);
                return;
            }

            if (args[1] != "-token")
            {
                UpdateClient(false);
                return;
            }

            if (!File.Exists(@path + @"\TadahApp.exe"))
            {
                MessageBox.Show("The client couldn't be found, so we are going to install it.\nCurrent Directory: " + path, "Client Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateClient(false);
                return;
            }

            label1.Text = "Checking version...";
            progressBar1.Value = 50;

            string recievedClientData;
            try
            {
                recievedClientData = new WebClient().DownloadString(baseUrl + "/client/" + client);
            }
            catch
            {
                label1.Text = "Error: Can't connect.";
                MessageBox.Show("Could not connect to Tadah.\n\nCurrent baseUrl: " + baseUrl + "\nLauncher version: " + version + "\n\nIf this persists, please send this message to the developers.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            string versionString = "false";
            string downloadUrl = "false";
            string sha512 = "none";

            try
            {
                JObject clientData = JObject.Parse(recievedClientData);

                versionString = (string)clientData["version"];
                downloadUrl = (string)clientData["url"];
                sha512 = (string)clientData["sha512"];
            }
            catch
            {
                MessageBox.Show("Could not parse client info JSON data. This usually occurs if the webserver gives an invalid response. Contact the developers if this issue persists.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }
            
            if (versionString == version)
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
                UpdateClient(false);
                return;
            }
        }
    }
}
