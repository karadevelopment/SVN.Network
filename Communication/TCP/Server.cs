using SVN.Debug;
using SVN.Tasks;
using System;
using System.Net;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Server : Controller
    {
        private TcpListener TcpListener { get; set; }
        public Action<int, string> Handle { get; set; } = (x, y) => { };

        public Server()
        {
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

        private void HandleObject(int id, string json)
        {
            this.Handle(id, json);
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