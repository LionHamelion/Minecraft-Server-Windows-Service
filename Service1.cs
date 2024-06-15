using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Configuration;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerService
{
    public partial class Service1 : ServiceBase
    {
        private Process minecraftProcess;
        private readonly string javaPath;
        private readonly string serverJarPath;
        private readonly string serverArgs;
        private readonly string logFilePath;
        private PipeHelper pipeHelper;

        public Service1()
        {
            InitializeComponent();
            javaPath = ConfigurationManager.AppSettings["JavaPath"];
            serverJarPath = ConfigurationManager.AppSettings["ServerJarPath"];
            serverArgs = ConfigurationManager.AppSettings["ServerArgs"];
            logFilePath = ConfigurationManager.AppSettings["LogFilePath"];
            pipeHelper = new PipeHelper(OnInputReceived, Log);
        }

        protected override void OnStart(string[] args)
        {
            Log("Service starting...");
            StartMinecraftServer();
            pipeHelper.StartPipeServers();
        }

        protected override void OnStop()
        {
            Log("Service stopping...");
            StopMinecraftServer();
            pipeHelper.StopPipeServers();
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
                            WorkingDirectory = Path.GetDirectoryName(serverJarPath),
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            RedirectStandardInput = true
                        }
                    };

                    minecraftProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            pipeHelper.WriteToPipe(e.Data);
                            Log($"Minecraft output: {e.Data}");
                        }
                    };
                    minecraftProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            pipeHelper.WriteToPipe(e.Data);
                            Log($"Minecraft error: {e.Data}");
                        }
                    };

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

        private void OnInputReceived(string input)
        {
            try
            {
                if (minecraftProcess != null && !minecraftProcess.HasExited)
                {
                    minecraftProcess.StandardInput.WriteLine(input);
                    Log($"Command sent to Minecraft: {input}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error sending command to Minecraft: {ex.Message}");
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

