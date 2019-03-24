using SVN.Network.Communication.Message;
using SVN.Tasks;
using System;
using System.Net;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Server : Controller, IDisposable
    {
        private TcpListener TcpListener { get; set; }

        public int ConnectedClients
        {
            get => base.ConnectionsCount;
        }

        public Server()
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        public void Start(int port = 10000)
        {
            this.TcpListener = new TcpListener(IPAddress.Any, port);
            this.TcpListener.Start();

            TaskContainer.Run(() => this.Listener(port));
        }

        public new void Stop()
        {
            this.TcpListener.Stop();
            base.Stop();
        }

        public new void Send(int clientId, IMessage message)
        {
            base.Send(clientId, message);
        }

        private void Listener(int port)
        {
            while (base.IsRunning)
            {
                try
                {
                    base.Start(this.TcpListener.AcceptTcpClient(), false);
                }
                catch (Exception e)
                {
                    base.LogException(e);
                    this.Stop();
                }
            }
        }
    }
}