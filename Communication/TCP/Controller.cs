using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SVN.Network.Communication.TCP
{
    public class Controller
    {
        public bool IsRunning { get; protected set; }
        private List<Connection> Connections { get; } = new List<Connection>();

        protected Controller()
        {
        }

        protected void Start(TcpClient tcpClient, Action<int, string> handle)
        {
            lock (this.Connections)
            {
                this.Connections.Add(new Connection(this, tcpClient, handle));
            }
        }

        protected void StopAll()
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections)
                {
                    connection.Dispose();
                }
            }
        }

        protected void SendObject(string json)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.IsRunning))
                {
                    connection.SendObject(json);
                }
            }
        }

        protected void SendObject(int id, string json)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.Id == id && x.IsRunning))
                {
                    connection.SendObject(json);
                }
            }
        }
    }
}