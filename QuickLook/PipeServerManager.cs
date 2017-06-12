// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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