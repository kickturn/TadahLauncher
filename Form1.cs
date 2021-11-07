using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

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
                label1.Text = "Installing Tadah...";

                // download zip... check sha512... extract zip... (optional) redirect to servers page
            }
            else
            {
                // Clone the installer to the temp folder so we can actually install.

                string currentExecutablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string copiedExecutablePath = Path.GetTempPath() + "TadahLauncher.exe";

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
