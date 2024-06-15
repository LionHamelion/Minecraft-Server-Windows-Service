using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Server_Console_App
{
    class Program
    {
        private static NamedPipeClientStream inputPipeClient;
        private static NamedPipeClientStream outputPipeClient;
        private static StreamWriter inputWriter;
        private static StreamReader outputReader;
        private static bool isRunning;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelHandler);
            isRunning = true;

            while (isRunning)
            {
                try
                {
                    Console.WriteLine("Attempting to connect to pipes...");
                    inputPipeClient = new NamedPipeClientStream(".", "MinecraftServerInputPipe", PipeDirection.InOut, PipeOptions.Asynchronous);
                    outputPipeClient = new NamedPipeClientStream(".", "MinecraftServerOutputPipe", PipeDirection.In, PipeOptions.Asynchronous);

                    inputPipeClient.Connect();
                    outputPipeClient.Connect();


                    Console.WriteLine("Connected to pipes.");

                    inputWriter = new StreamWriter(inputPipeClient) { AutoFlush = true };
                    outputReader = new StreamReader(outputPipeClient);


                    Thread readThread = new Thread(ReadOutput);
                    readThread.Start();

                    string input;
                    Console.WriteLine("Connected to Minecraft server. Type commands to send:");
                    while (isRunning && (input = Console.ReadLine()) != null)
                    {
                        inputWriter.WriteLine(input);
                        Console.WriteLine($"Sent command: {input}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    CleanUp();
                    if (isRunning)
                    {
                        Console.WriteLine("Reconnecting...");
                        Thread.Sleep(2000); // Wait a bit before attempting to reconnect
                    }
                }
            }
        }

        private static void ReadOutput()
        {
            try
            {
                string line;
                while (isRunning && (line = outputReader.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Read output error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Output reader disconnected.");
            }
        }

        private static void CancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            isRunning = false;
            CleanUp();
            Environment.Exit(0);
        }

        private static void CleanUp()
        {
            try
            {
                inputWriter?.Close();
                outputReader?.Close();
                inputPipeClient?.Close();
                outputPipeClient?.Close();
                Console.WriteLine("Cleaned up resources.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Clean up error: {ex.Message}");
            }
        }
    }
}


