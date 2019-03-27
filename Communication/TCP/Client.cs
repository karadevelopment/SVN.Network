using SVN.Network.Communication.Message;
using System;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Client : Controller
    {
        public static Client Instance { get; } = new Client();

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

        public new bool IsRunning
        {
            get => base.IsRunning && base.HasRunningConnection;
        }

        public Client()
        {
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
                base.OnInitializationSuccess(ip, port);
            }
            catch (Exception e)
            {
                base.OnInitializationFailed(ip, port, e);
                this.Stop();
            }
        }

        public void Send(IMessage message)
        {
            base.SendToAll(message);
        }
    }
}