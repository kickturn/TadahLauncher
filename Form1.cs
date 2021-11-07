using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace TadahLauncher
{
    public partial class Form1 : Form
    {
        static string baseUrl = "http://tadah.rocks";
        static string version = "1.0.6";
        static string client = "2010";
        static string installPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Tadah\\" + client;
        static bool doSha256Check = false;

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
            string sha256 = "none";

            try
            {
                JObject clientData = JObject.Parse(recievedClientData);

                versionString = (string)clientData["version"];
                downloadUrl = (string)clientData["url"];
                sha256 = (string)clientData["sha256"];
            }
            catch
            {
                MessageBox.Show("Could not parse client info JSON data. This usually occurs if the webserver gives an invalid response. Contact the developers if this issue persists.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            if (sha256 == "none")
            {
                MessageBox.Show("The client exists on the webserver, but it is not downloadable anymore. If you believe this is an error, contact the developers. (sha256 is \"none\").", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            if (doInstall)
            {
                // Assuming we're in the temp folder or the user knows what they're doing, start replacing files.
                label1.Text = "Downloading latest client...";

                WebClient webClient = new WebClient();
                string tempZipArchivePath = Path.GetTempPath() + "Tadah" + client + ".zip";

                webClient.DownloadProgressChanged += (s, e) =>
                {
                    progressBar1.Value = e.ProgressPercentage;
                };

                webClient.DownloadFileCompleted += (s, e) =>
                {
                    if (doSha256Check)
                    {
                        label1.Text = "Verifying downloaded files...";

                        SHA256 cSha256 = SHA256.Create();

                        byte[] zipArchiveSha256Bytes;
                        using (FileStream stream = File.OpenRead(tempZipArchivePath))
                        {
                            zipArchiveSha256Bytes = cSha256.ComputeHash(stream);
                        }

                        string sha256result = "";
                        foreach (byte b in zipArchiveSha256Bytes) sha256result += b.ToString("x2");

                        if (sha256result != sha256)
                        {
                            MessageBox.Show("SHA256 mismatch.\nWebsite reported: " + sha256 + "\nLocal file: " + sha256result, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Close();
                            return;
                        }
                    }

                    label1.Text = "Extracting files...";
                    progressBar1.Value = 50;

                    try
                    {
                        if (Directory.Exists(installPath))
                        {
                            Directory.Delete(installPath, true);
                        }

                        ZipFile.ExtractToDirectory(tempZipArchivePath, installPath);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Error occurred while attempting to extract the client to its proper directory. (" + exc.Message + ") \n\n" + installPath, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                        return;
                    }

                    label1.Text = "Setting up URI...";
                    try
                    {
                        var classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true);

                        var key = classesKey.CreateSubKey("tadahten");
                        key.CreateSubKey("DefaultIcon").SetValue("", installPath + "\\TadahLauncher.exe,1");
                        key.SetValue("", "tadahten:Protocol");
                        key.SetValue("URL Protocol", "");
                        key.CreateSubKey(@"shell\open\command").SetValue("", installPath + "\\TadahLauncher.exe -token %1");
                        key.Close();
                    }
                    catch
                    {
                        MessageBox.Show("Could not set up Tadah's URI. This usually happens on machines running Windows 7 or older. Tadah may not launch at all.", "URI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    label1.Text = "Clean up...";
                    if (Directory.Exists("C:\\Tadah"))
                    {
                        DialogResult dialogResult = MessageBox.Show("Old Tadah installation folder detected. Would you like to delete the old Tadah installation folder (found at C:\\Tadah)?\n\nPlease make sure you aren't leaving anything important behind, because that data cannot be restored when you confirm you press \"Yes.\"", "Clean Up", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dialogResult == DialogResult.Yes)
                        {
                            try
                            {
                                Directory.Delete("C:\\Tadah", true);

                                MessageBox.Show("Old Tadah folder deleted.", "Clean Up", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch
                            {
                                MessageBox.Show("Could not delete the entirety of the old installation folder. There may be files remaining, so please go clean those up yourself.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }

                    if (File.Exists(tempZipArchivePath))
                    {
                        File.Delete(tempZipArchivePath);
                    }

                    MessageBox.Show("Tadah successfully updated. Go ahead and play.", "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                    return;
                };

                try
                {
                    webClient.DownloadFileAsync(new Uri(baseUrl + "/client/download/" + client), tempZipArchivePath);
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

            if (!File.Exists(installPath + "\\TadahApp.exe"))
            {
                MessageBox.Show("The client couldn't be found, so we are going to install it.\nInstall Path: " + installPath, "Client Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string sha256 = "none";

            try
            {
                JObject clientData = JObject.Parse(recievedClientData);

                versionString = (string)clientData["version"];
                downloadUrl = (string)clientData["url"];
                sha256 = (string)clientData["sha256"];
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
                        FileName = installPath + "\\TadahApp.exe",
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
                label1.Text = "New version found, updating: " + version + " -> " + versionString;
                await Task.Delay(5000);
                UpdateClient(false);
                return;
            }
        }
    }
}
