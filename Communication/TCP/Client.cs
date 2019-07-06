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

        public void Start(string ip = "localhost", int port = 10000)
        {
            try
            {
                var tcpClient = new TcpClient(ip, port)
                {
                    ReceiveTimeout = (int)this.Timeout.TotalMilliseconds,
                };

                base.Start();
                base.StartConnection(tcpClient);
                base.OnInitializationSuccess(ip, port);
            }
            catch (Exception e)
            {
                base.OnInitializationFailed(ip, port, e);
                base.Stop();
            }
        }

        public void Send(object message)
        {
            base.SendToAll(message);
        }
    }
}