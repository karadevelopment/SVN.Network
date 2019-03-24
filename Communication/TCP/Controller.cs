using SVN.Core.Linq;
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

        protected int ConnectionsCount
        {
            get => this.Connections.Count;
        }

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

        internal void Stop(Connection connection)
        {
            lock (this.Connections)
            {
                this.Connections.Remove(connection);
            }
        }

        protected void StopAll()
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Copy())
                {
                    connection.Dispose();
                }
            }
        }

        protected void SendObject(string message)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.IsRunning))
                {
                    connection.SendObject(message);
                }
            }
        }

        protected void SendObject(int clientId, string message)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.Id == clientId && x.IsRunning))
                {
                    connection.SendObject(message);
                }
            }
        }
    }
}