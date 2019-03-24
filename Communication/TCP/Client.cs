using SVN.Debug;
using System;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Client : Controller, IDisposable
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public Action<string> Handle { get; set; }

        public Client()
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        public void Start(string ip = "localhost", int port = 10000)
        {
            base.IsRunning = true;

            var tcpClient = new TcpClient(ip, port)
            {
                ReceiveTimeout = (int)this.Timeout.TotalMilliseconds,
            };
            base.Start(tcpClient, this.HandleObject);

            Logger.Write($"connected to {ip}:{port}");
        }

        public void Stop()
        {
            base.IsRunning = false;
            base.StopAll();
        }

        public void Send(string json)
        {
            base.SendObject(json);
        }

        private void HandleObject(int id, string json)
        {
            this.Handle?.Invoke(json);
        }
    }
}