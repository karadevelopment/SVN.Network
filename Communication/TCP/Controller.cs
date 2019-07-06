using SVN.Core.Format;
using SVN.Core.Linq;
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
        private List<Connection> Connections { get; } = new List<Connection>();
        public long UpTraffic { get; internal set; }
        public long DownTraffic { get; internal set; }

        public Action<string, int> OnInitializationSuccess { get; set; } = (ip, port) => { };
        public Action<string, int, Exception> OnInitializationFailed { get; set; } = (ip, port, exception) => { };
        public Action<int> OnConnectionStart { get; set; } = exception => { };
        public Action<int> OnConnectionStop { get; set; } = exception => { };
        public Action<int, string> OnConnectionReceivedMessage { get; set; } = (clientId, message) => { };
        public Action<int, string> OnConnectionSentMessage { get; set; } = (clientId, message) => { };
        public Action<int, object> OnConnectionHandleMessage { get; set; } = (clientId, message) => { };
        public Action<Exception> OnUnhandledException { get; set; } = exception => { };
        public Action<object> OnLog { get; set; } = message => { };

        public int ConnectionsAlive
        {
            get => this.Connections.Count;
        }

        public bool HasRunningConnection
        {
            get
            {
                lock (this.Connections)
                {
                    return this.Connections.Any(x => x.IsRunning);
                }
            }
        }

        public string UpTrafficText
        {
            get => this.UpTraffic.FormatByteSize();
        }

        public string DownTrafficText
        {
            get => this.DownTraffic.FormatByteSize();
        }

        private TimeSpan SleepTime
        {
            get => TimeSpan.FromMilliseconds(10);
        }

        protected Controller()
        {
            TaskContainer.ExceptionHandler = this.OnUnhandledException;
        }

        protected void Start()
        {
            if (!this.IsRunning)
            {
                this.IsRunning = true;
                TaskContainer.Run(this.Observer);
            }
        }

        public void Stop()
        {
            if (this.IsRunning)
            {
                this.IsRunning = false;
                this.StopConnection();
            }
        }

        public void Reset()
        {
            this.StopConnection();
        }

        internal void StartConnection(TcpClient tcpClient)
        {
            this.StartConnection(new Connection(this, tcpClient));
        }

        internal void StartConnection(Connection connection)
        {
            lock (this.Connections)
            {
                connection.Start();
                this.Connections.Add(connection);
            }
        }

        internal void StopConnection()
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Copy())
                {
                    connection.Stop();
                }
            }
        }

        internal void StopConnection(Connection connection)
        {
            lock (this.Connections)
            {
                connection.Stop();
            }
        }

        protected void Send(int clientId, object message)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.Id == clientId && x.IsRunning))
                {
                    connection.Send(message);
                }
            }
        }

        protected void SendToOthers(int clientId, object message)
        {
            lock (this.Connections)
            {
                foreach (var connection in this.Connections.Where(x => x.Id != clientId && x.IsRunning))
                {
                    connection.Send(message);
                }
            }
        }

        protected void SendToAll(object message)
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
                    foreach (var connection in this.Connections.Where(x => !x.IsRunning).Copy())
                    {
                        this.Connections.Remove(connection);
                    }
                }

                Thread.Sleep(this.SleepTime);
            }
        }
    }
}