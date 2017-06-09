using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace QuickLook
{
    internal class PipeServerManager : IDisposable
    {
        private const string PipeName = "QuickLook.App.Pipe";
        private const string PipeCloseMessage = "QuickLook.App.Pipe.QuitSingal";
        private static PipeServerManager _instance;

        private NamedPipeServerStream _server;

        public PipeServerManager()
        {
            _server = new NamedPipeServerStream(PipeName, PipeDirection.In);

            new Task(() =>
            {
                using (var reader = new StreamReader(_server))
                {
                    while (true)
                    {
                        Debug.WriteLine("PipeManager: WaitForConnection");

                        _server.WaitForConnection();
                        var msg = reader.ReadLine();

                        Debug.WriteLine($"PipeManager: {msg}");

                        if (msg == PipeCloseMessage)
                            return;

                        // dispatch message
                        MessageReceived?.Invoke(msg, new EventArgs());

                        _server.Disconnect();
                    }
                }
            }).Start();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_server != null)
                SendMessage(PipeCloseMessage);
            _server?.Dispose();
            _server = null;
        }

        public event EventHandler MessageReceived;

        public static void SendMessage(string msg)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect();

                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine(msg);
                        writer.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        ~PipeServerManager()
        {
            Dispose();
        }

        public static PipeServerManager GetInstance()
        {
            return _instance ?? (_instance = new PipeServerManager());
        }
    }
}