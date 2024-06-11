using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerService
{
    public partial class Service1 : ServiceBase
    {
        private Process minecraftProcess;
        private readonly string javaPath = @"C:\Program Files\Java\jdk-22\bin\java.exe";
        private readonly string serverJarPath = @"D:\Games\Minecraft\server-1.21-release-candidate-1\minecraft-server-1.21-release-candidate-1.jar";
        private readonly string serverArgs = "-Xms1024M -Xmx2048M -jar minecraft-server-1.21-release-candidate-1.jar nogui";
        private readonly string logFilePath = @"D:\Games\Minecraft\server-1.21-release-candidate-1\minecraft_server_log.txt";

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Log("Service starting...");
            StartMinecraftServer();
        }

        protected override void OnStop()
        {
            Log("Service stopping...");
            StopMinecraftServer();
        }

        private void StartMinecraftServer()
        {
            try
            {
                if (minecraftProcess == null)
                {
                    minecraftProcess = new Process
                    {
                        StartInfo =
                        {
                            FileName = javaPath,
                            Arguments = serverArgs,
                            WorkingDirectory = System.IO.Path.GetDirectoryName(serverJarPath),
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    minecraftProcess.OutputDataReceived += (sender, e) => Log(e.Data);
                    minecraftProcess.ErrorDataReceived += (sender, e) => Log(e.Data);

                    minecraftProcess.Start();
                    minecraftProcess.BeginOutputReadLine();
                    minecraftProcess.BeginErrorReadLine();

                    Log("Minecraft server started.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error starting Minecraft server: {ex.Message}");
            }
        }

        private void StopMinecraftServer()
        {
            try
            {
                if (minecraftProcess != null && !minecraftProcess.HasExited)
                {
                    minecraftProcess.Kill();
                    minecraftProcess = null;
                    Log("Minecraft server stopped.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error stopping Minecraft server: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occur when trying to write to the log file.
                // Consider using EventLog in a real-world scenario.
            }
        }
    }
}
