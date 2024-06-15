using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MinecraftServerService
{
    public class PipeHelper
    {
        private NamedPipeServerStream inputPipeServer;
        private NamedPipeServerStream outputPipeServer;
        private StreamWriter outputPipeWriter;
        private Action<string> onInputReceived;
        private Action<string> logAction;
        private bool isInputPipeRunning;
        private bool isOutputPipeRunning;

        public PipeHelper(Action<string> onInputReceived, Action<string> logAction)
        {
            this.onInputReceived = onInputReceived;
            this.logAction = logAction;
        }

        public void StartPipeServers()
        {
            isInputPipeRunning = true;
            isOutputPipeRunning = true;

            Task.Run(() => HandleInputPipe());
            Task.Run(() => HandleOutputPipe());
        }

        private void HandleInputPipe()
        {
            while (isInputPipeRunning)
            {
                try
                {
                    logAction("Waiting for input pipe connection...");
                    inputPipeServer = new NamedPipeServerStream("MinecraftServerInputPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    inputPipeServer.WaitForConnection();
                    logAction("Input pipe connected.");

                    using (var reader = new StreamReader(inputPipeServer))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            onInputReceived(line);
                        }
                    }
                }
                catch (IOException ex)
                {
                    logAction($"Input pipe error: {ex.Message}");
                }
                finally
                {
                    ClosePipe(ref inputPipeServer);
                    logAction("Input pipe disconnected.");
                }
            }
        }

        private void HandleOutputPipe()
        {
            while (isOutputPipeRunning)
            {
                try
                {
                    logAction("Waiting for output pipe connection...");
                    outputPipeServer = new NamedPipeServerStream("MinecraftServerOutputPipe", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    outputPipeServer.WaitForConnection();
                    outputPipeWriter = new StreamWriter(outputPipeServer) { AutoFlush = true };
                    logAction("Output pipe connected.");

                    // Wait for the output pipe to be closed
                    while (outputPipeServer.IsConnected)
                    {
                        Thread.Sleep(100); // Sleep to prevent busy wait
                    }
                }
                catch (IOException ex)
                {
                    logAction($"Output pipe error: {ex.Message}");
                }
                finally
                {
                    ClosePipe(ref outputPipeServer);
                    logAction("Output pipe disconnected.");
                }
            }
        }

        public void StopPipeServers()
        {
            logAction("Stopping pipe servers...");

            isInputPipeRunning = false;
            isOutputPipeRunning = false;

            ClosePipe(ref inputPipeServer);
            ClosePipe(ref outputPipeServer);

            logAction("Pipe servers stopped.");
        }

        public void WriteToPipe(string message)
        {
            try
            {
                if (outputPipeWriter != null && outputPipeServer.IsConnected && message != null)
                {
                    outputPipeWriter.WriteLine(message);
                    logAction($"Written to output pipe: {message}");
                }
            }
            catch (IOException ex)
            {
                logAction($"Write to pipe error: {ex.Message}");
            }
        }

        private void ClosePipe(ref NamedPipeServerStream pipe)
        {
            if (pipe != null)
            {
                try
                {
                    pipe.Close();
                }
                catch (IOException ex)
                {
                    logAction($"Error closing pipe: {ex.Message}");
                }
                finally
                {
                    pipe = null;
                }
                logAction("Pipe closed.");
            }
        }
    }
}




