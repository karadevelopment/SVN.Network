using Newtonsoft.Json;
using SVN.Network.Communication.Message;
using SVN.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace SVN.Network.Communication.TCP
{
    internal class Connection : IDisposable
    {
        private static int Counter { get; set; }
        public int Id { get; } = ++Connection.Counter;
        public bool IsRunning { get; private set; }
        private Controller Controller { get; }
        private TcpClient TcpClient { get; }
        private NetworkStream NetworkStream { get; }
        private StreamReader StreamReader { get; }
        private StreamWriter StreamWriter { get; }
        private List<IMessage> Input { get; } = new List<IMessage>();
        private List<IMessage> Output { get; } = new List<IMessage>();

        private JsonSerializerSettings SerializerSettings
        {
            get => new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
            };
        }

        private TimeSpan SleepTime
        {
            get => TimeSpan.FromMilliseconds(10);
        }

        private TimeSpan PingTime
        {
            get => TimeSpan.FromMinutes(1);
        }

        public Connection(Controller controller, TcpClient tcpClient, bool sendPings)
        {
            this.IsRunning = true;

            this.Controller = controller;
            this.TcpClient = tcpClient;
            this.NetworkStream = this.TcpClient.GetStream();
            this.StreamReader = new StreamReader(this.NetworkStream);
            this.StreamWriter = new StreamWriter(this.NetworkStream);

            TaskContainer.Run(this.Receiver);
            TaskContainer.Run(this.Sender);
            TaskContainer.Run(this.Handler);

            if (sendPings)
            {
                TaskContainer.Run(this.PingSender);
            }

            this.Controller.LogConnectionEvent(this.Id, "connected");
        }

        public void Dispose()
        {
            if (this.IsRunning)
            {
                this.IsRunning = false;

                this.StreamReader?.Close();
                this.StreamReader?.Dispose();
                this.StreamWriter?.Close();
                this.StreamWriter?.Dispose();
                this.NetworkStream?.Close();
                this.NetworkStream?.Dispose();
                this.TcpClient?.Close();
                this.TcpClient?.Dispose();

                this.Controller.LogConnectionEvent(this.Id, "disconnected");
            }
        }

        public void Send(IMessage message)
        {
            lock (this.Output)
            {
                this.Output.Add(message);
            }
        }

        private void Receiver()
        {
            while (this.Controller.IsRunning && this.IsRunning)
            {
                try
                {
                    var data = this.StreamReader.ReadLine();

                    if (string.IsNullOrWhiteSpace(data))
                    {
                        Thread.Sleep(this.SleepTime);
                        continue;
                    }

                    var message = JsonConvert.DeserializeObject<IMessage>(data, this.SerializerSettings);

                    lock (this.Input)
                    {
                        this.Input.Add(message);
                    }

                    this.Controller.LogConnectionTransfer(this.Id, $"received data: {data}");
                    Thread.Sleep(this.SleepTime);
                }
                catch (IOException)
                {
                }
                catch (SocketException)
                {
                    this.Dispose();
                }
                catch (Exception e)
                {
                    this.Controller.LogConnectionException(this.Id, e);
                    this.Dispose();
                }
            }
        }

        private void Sender()
        {
            while (this.Controller.IsRunning && this.IsRunning)
            {
                try
                {
                    if (!this.Input.Any())
                    {
                        Thread.Sleep(this.SleepTime);
                        continue;
                    }

                    var message = default(IMessage);

                    lock (this.Output)
                    {
                        message = this.Output.ElementAt(0);
                        this.Output.RemoveAt(0);
                    }

                    var data = JsonConvert.SerializeObject(message, this.SerializerSettings);

                    this.StreamWriter.WriteLine(data);
                    this.StreamWriter.Flush();

                    this.Controller.LogConnectionTransfer(this.Id, $"sent data: {data}");
                    Thread.Sleep(this.SleepTime);
                }
                catch (SocketException)
                {
                    this.Dispose();
                }
                catch (Exception e)
                {
                    this.Controller.LogConnectionException(this.Id, e);
                    this.Dispose();
                }
            }
        }

        private void Handler()
        {
            while (this.Controller.IsRunning && this.IsRunning)
            {
                try
                {
                    if (!this.Input.Any())
                    {
                        Thread.Sleep(this.SleepTime);
                        continue;
                    }

                    var message = default(IMessage);

                    lock (this.Input)
                    {
                        message = this.Input.ElementAt(0);
                        this.Input.RemoveAt(0);
                    }

                    if (message is Ping)
                    {
                        this.Controller.LogConnectionPing(this.Id, "ping");
                        this.Send(new Pong());
                    }
                    else if (message is Pong)
                    {
                        this.Controller.LogConnectionPing(this.Id, "pong");
                    }
                    else
                    {
                        this.Controller.HandleMessage(this.Id, message);
                    }

                    Thread.Sleep(this.SleepTime);
                }
                catch (Exception e)
                {
                    this.Controller.LogConnectionException(this.Id, e);
                }
            }
        }

        private void PingSender()
        {
            var stopwatch = Stopwatch.StartNew();

            while (this.Controller.IsRunning && this.IsRunning)
            {
                if (this.PingTime < stopwatch.Elapsed)
                {
                    this.Send(new Ping());
                    stopwatch.Restart();
                }

                Thread.Sleep(this.SleepTime);
            }
        }
    }
}