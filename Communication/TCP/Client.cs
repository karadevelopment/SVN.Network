using SVN.Network.Communication.Message;
using System;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Client : Controller, IDisposable
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public Client()
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        public void Start(string ip = "localhost", int port = 10000)
        {
            var tcpClient = new TcpClient(ip, port)
            {
                ReceiveTimeout = (int)this.Timeout.TotalMilliseconds,
            };
            base.Start(tcpClient);
            base.LogEvent($"connected to {ip}:{port}");
        }

        public new void Stop()
        {
            base.Stop();
        }

        public new void Send(IMessage message)
        {
            base.Send(message);
        }
    }
}