using SVN.Core.Format;
using SVN.Core.Linq;
using SVN.Network.Communication.Message;
using SVN.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace SVN.Network.Communication.TCP
{
    public class Controller
    {
        internal bool IsRunning { get; private set; }
        internal bool HasRunningConnection { get; private set; }
        private List<Connection> Connections { get; } = new List<Connection>();
        public long UpStream { get; internal set; }
        public long DownStream { get; internal set; }
        public Action<int, IMessage> HandleMessage { get; set; } = (clientId, message) => { };
        public Action<string> HandleEvent { get; set; } = message => { };
        public Action<Exception> HandleException { get; set; } = exception => { };
        public Action<int> HandleConnectionStart { get; set; } = clientId => { };
        public Action<int> HandleConnectionEnd { get; set; } = clientId => { };
        public Action<int, string> HandleConnectionEvent { get; set; } = (clientId, message) => { };
        public Action<int, string> HandleConnectionTransfer { get; set; } = (clientId, message) => { };
        public Action<int, Exception> HandleConnectionException { get; set; } = (clientId, exception) => { };

        protected int ConnectionsCount
        {
            get => this.Connections.Count;
        }

        public string DownStreamText
        {
            get => this.DownStream.FormatByteSize();
        }

        public string UpStreamText
        {
            get => this.UpStream.FormatByteSize();
        }

        private TimeSpan SleepTime
        {
            get => TimeSpan.FromMilliseconds(10);
        }

        protected Controller()
        {
        }

        protected IEnumerable<int> GetConnectionIdsAsync()
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.IsRunning))
                {
                    yield return connection.Id;
                }
            }
        }

        protected void Start(TcpClient tcpClient, bool sendPings)
        {
            this.IsRunning = true;

            lock (this.Connections)
            {
                this.Connections.Add(new Connection(this, tcpClient, sendPings));
                this.HasRunningConnection = this.Connections.Any(x => x.IsRunning);
            }

            TaskContainer.Run(this.Observer);
        }

        protected void Stop()
        {
            if (this.IsRunning)
            {
                this.IsRunning = false;

                lock (this.Connections)
                {
                    foreach (var connection in this.Connections.Copy())
                    {
                        connection.Dispose();
                        this.Connections.Remove(connection);
                    }
                }
            }
        }

        protected void Reset()
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Copy())
                {
                    connection.Dispose();
                    this.Connections.Remove(connection);
                }
            }
        }

        protected void Send(int clientId, IMessage message)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.Id == clientId && x.IsRunning))
                {
                    connection.Send(message);
                }
            }
        }

        protected void SendOthers(int clientId, IMessage message)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.Id != clientId && x.IsRunning))
                {
                    connection.Send(message);
                }
            }
        }

        protected void SendAll(IMessage message)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.IsRunning))
                {
                    connection.Send(message);
                }
            }
        }

        private void Observer()
        {
            while (this.IsRunning)
            {
                lock (this.Connections)
                {
                    this.HasRunningConnection = this.Connections.Any(x => x.IsRunning);

                    foreach (var connection in this.Connections.Where(x => !x.IsRunning).Copy())
                    {
                        connection.Dispose();
                        this.Connections.Remove(connection);
                    }
                }

                Thread.Sleep(this.SleepTime);
            }
        }
    }
}