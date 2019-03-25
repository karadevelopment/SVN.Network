using SVN.Network.Communication.Message;
using SVN.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Server : Controller, IDisposable
    {
        public new bool IsRunning { get; private set; }
        private TcpListener TcpListener { get; set; }

        public int Clients
        {
            get => base.ConnectionsCount;
        }

        public List<int> ClientIds
        {
            get => base.GetConnectionIdsAsync().ToList();
        }

        public Server()
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        public void Start(int port = 10000, bool sendPings = false)
        {
            this.IsRunning = true;

            this.TcpListener = new TcpListener(IPAddress.Any, port);
            this.TcpListener.Start();

            TaskContainer.Run(() => this.Listener(port, sendPings));
        }

        public new void Stop()
        {
            if (this.IsRunning)
            {
                this.IsRunning = false;
                this.TcpListener.Stop();
                base.Stop();
            }
        }

        public new void Send(int clientId, IMessage message)
        {
            base.Send(clientId, message);
        }

        public new void SendOthers(int clientId, IMessage message)
        {
            base.SendOthers(clientId, message);
        }

        public new void SendAll(IMessage message)
        {
            base.SendAll(message);
        }

        private void Listener(int port, bool sendPings)
        {
            while (this.IsRunning)
            {
                try
                {
                    base.Start(this.TcpListener.AcceptTcpClient(), sendPings);
                }
                catch (SocketException)
                {
                    this.Stop();
                }
                catch (Exception e)
                {
                    base.HandleException(e);
                    this.Stop();
                }
            }
        }
    }
}