using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MetaQuestPatcher
{
    public partial class Form1 : Form
    {
        private Process cmdProcess;
        private string lastCmd = "";
        private string processName = "";

        private string[] adbCommands = {
            "cd platform-tools",
            "adb push cheese /data/local/tmp",
            "adb push frida-server /data/local/tmp",
            "adb shell mv /data/local/tmp/cheese /data/local/tmp/cheese",
            "adb shell mv /data/local/tmp/frida-server /data/local/tmp/frida-server",
            "adb shell chmod +x /data/local/tmp/cheese",
            "adb shell chmod +x /data/local/tmp/frida-server",
            "adb shell",
            "/data/local/tmp/cheese",
            "/data/local/tmp/frida-server &"
        };

        public Form1()
        {
            InitializeComponent();
        }

        private void SetupCmd()
        {
            cmdProcess = new Process();
            cmdProcess.StartInfo.FileName = "cmd.exe";
            cmdProcess.StartInfo.UseShellExecute = false;
            cmdProcess.StartInfo.RedirectStandardInput = true;
            cmdProcess.StartInfo.RedirectStandardOutput = true;
            cmdProcess.StartInfo.RedirectStandardError = true;
            cmdProcess.StartInfo.CreateNoWindow = true;

            cmdProcess.OutputDataReceived += (sender, e) => {
                if (e.Data != null && e.Data != "")
                {
                    string output = e.Data;

                    if (output.Contains(">"))
                    {
                        int promptIndex = output.LastIndexOf(">");
                        if (promptIndex >= 0 && promptIndex < output.Length - 1)
                        {
                            output = output.Substring(promptIndex + 1).TrimStart();
                        }
                        else
                        {
                            output = "";
                        }
                    }

                    if (output.Trim() != "" && output != lastCmd)
                    {
                        UpdateOutput(output + "\r\n");
                    }
                }
            };

            cmdProcess.ErrorDataReceived += (sender, e) => {
                if (e.Data != null && e.Data != "")
                {
                    UpdateOutput("[ERROR] " + e.Data + "\r\n");
                }
            };

            cmdProcess.Start();
            cmdProcess.BeginOutputReadLine();
            cmdProcess.BeginErrorReadLine();
        }

        private void ExecuteCommand(string cmd)
        {
            if (cmdProcess != null && !cmdProcess.HasExited)
            {
                lastCmd = cmd;
                cmdProcess.StandardInput.WriteLine(cmd);
                cmdProcess.StandardInput.Flush();
            }
        }

        private async Task ExecuteAllCommands()
        {
            for (int i = 0; i < adbCommands.Length; i++)
            {
                ExecuteCommand(adbCommands[i]);
                await Task.Delay(500);
            }
        }

        private void UpdateOutput(string text)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(() => richTextBox1.AppendText(text)));
            }
            else
            {
                richTextBox1.AppendText(text);
            }
        }

        private async Task DownloadAdbStuff()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string toolsPath = Path.Combine(currentDir, "platform-tools");

            loadingProgressBar.Value = 10;

            if (!Directory.Exists(toolsPath))
            {
                string zipFile = Path.Combine(currentDir, "adb.zip");

                using (WebClient wc = new WebClient())
                {
                    await wc.DownloadFileTaskAsync(
                        "https://dl.google.com/android/repository/platform-tools-latest-windows.zip",
                        zipFile
                    );
                }

                loadingProgressBar.Value = 40;

                ZipFile.ExtractToDirectory(zipFile, currentDir);
                File.Delete(zipFile);
            }

            if (!Directory.Exists(toolsPath))
            {
                throw new Exception("Failed to extract platform-tools!");
            }

            string cheeseFile = Path.Combine(toolsPath, "cheese");
            string fridaFile = Path.Combine(toolsPath, "frida-server");

            // download cheese
            using (WebClient wc = new WebClient())
            {
                await wc.DownloadFileTaskAsync(
                    "https://drive.iidk.online/src/Quest3-Root/cheese",
                    cheeseFile
                );
            }

            loadingProgressBar.Value = 70;

            // download frida
            using (WebClient wc = new WebClient())
            {
                await wc.DownloadFileTaskAsync(
                    "https://drive.iidk.online/src/Quest3-Root/frida-server",
                    fridaFile
                );
            }

            loadingProgressBar.Value = 100;
        }

        private void buttonStartAdb_Click(object sender, EventArgs e)
        {
            if (cmdProcess == null || cmdProcess.HasExited)
            {
                SetupCmd();
            }

            ExecuteAllCommands();
        }

        private void buttonInject_Click(object sender, EventArgs e)
        {
            processName = textBox1.Text;
            DoInjection();
        }

        private void DoInjection()
        {
            if (comboBox1.SelectedItem == null)
            {
                UpdateOutput("Select a mod first!\r\n");
                return;
            }

            string fridaExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Python\Python313\Scripts\frida.exe");
            string modFile = Path.Combine(Directory.GetCurrentDirectory(), "Mods", comboBox1.SelectedItem.ToString());

            string args = "/k \"\"" + fridaExe + "\" -U -l frida-il2cpp-bridge.js -l \"" + modFile + "\" \"" + processName + "\"\"";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = args;
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = false;

            UpdateOutput("Running: " + args + "\r\n");

            Process.Start(startInfo);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if (cmdProcess == null || cmdProcess.HasExited)
            {
                SetupCmd();
            }
            ExecuteAllCommands();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            processName = textBox1.Text;
            DoInjection();
        }

        private void guna2CircleButton3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void guna2CircleButton1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void guna2CircleButton2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private async void Form1_Load_1(object sender, EventArgs e)
        {
            string fridaPath = Path.Combine(Directory.GetCurrentDirectory(), "platform-tools", "frida-server");

            if (!File.Exists(fridaPath))
            {
                guna2Panel1.Visible = true;

                comboBox1.Visible = false;
                button1.Visible = false;
                button2.Visible = false;
                textBox1.Visible = false;
                richTextBox1.Visible = false;

                loadingProgressBar.Visible = true;
                loadingProgressBar.Value = 0;

                try
                {
                    await DownloadAdbStuff();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Download failed: " + ex.Message);
                    return;
                }
            }

            loadingProgressBar.Visible = false;
            comboBox1.Visible = true;
            button1.Visible = true;
            button2.Visible = true;
            textBox1.Visible = true;
            richTextBox1.Visible = true;

            string modsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Mods");
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
            }

            comboBox1.Items.Clear();
            string[] modFiles = Directory.GetFiles(modsFolder);
            foreach (string file in modFiles)
            {
                comboBox1.Items.Add(Path.GetFileName(file));
            }
        }
    }
}