using SVN.Network.Communication.Message;
using System;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Client : Controller, IDisposable
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

        public Client()
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        public void Start(string ip = "localhost", int port = 10000, bool sendPings = true)
        {
            try
            {
                var tcpClient = new TcpClient(ip, port)
                {
                    ReceiveTimeout = (int)this.Timeout.TotalMilliseconds,
                };

                base.Start(tcpClient, sendPings);
                base.HandleEvent($"connected to {ip}:{port}");
            }
            catch (SocketException)
            {
                base.HandleEvent("server is not available");
                this.Stop();
            }
            catch (Exception e)
            {
                base.HandleException(e);
                this.Stop();
            }
        }

        public new void Stop()
        {
            base.Stop();
        }

        public void Send(IMessage message)
        {
            base.SendAll(message);
        }
    }
}