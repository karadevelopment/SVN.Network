using SVN.Debug;
using SVN.Tasks;
using System;
using System.Net;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Server : Controller, IDisposable
    {
        private TcpListener TcpListener { get; set; }
        public Action<int, string> Handle { get; set; }

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
            base.IsRunning = true;

            this.TcpListener = new TcpListener(IPAddress.Any, port);
            this.TcpListener.Start();

            TaskContainer.Run(() => this.Listener(port));
        }

        public void Stop()
        {
            base.IsRunning = false;
            base.StopAll();
            this.TcpListener.Stop();
        }

        public void Send(int clientId, string message)
        {
            base.SendObject(clientId, message);
        }

        private void HandleObject(int clientId, string message)
        {
            this.Handle?.Invoke(clientId, message);
        }

        private void Listener(int port)
        {
            while (base.IsRunning)
            {
                try
                {
                    base.Start(this.TcpListener.AcceptTcpClient(), this.HandleObject);
                }
                catch (Exception e)
                {
                    Logger.Write($"exception in Server.Listener: {e.ToString()}");
                    this.Stop();
                }
            }
        }
    }
}