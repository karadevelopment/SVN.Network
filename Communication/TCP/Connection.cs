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
    internal class Connection
    {
        private static int Counter { get; set; }
        public int Id { get; } = ++Connection.Counter;
        public bool IsRunning { get; private set; }
        private Controller Controller { get; }
        private TcpClient TcpClient { get; }
        private NetworkStream NetworkStream { get; }
        private StreamReader StreamReader { get; }
        private StreamWriter StreamWriter { get; }
        private List<object> Input { get; } = new List<object>();
        private List<object> Output { get; } = new List<object>();

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

        public Connection(Controller controller, TcpClient tcpClient)
        {
            this.Controller = controller;
            this.TcpClient = tcpClient;
            this.NetworkStream = this.TcpClient.GetStream();
            this.StreamReader = new StreamReader(this.NetworkStream);
            this.StreamWriter = new StreamWriter(this.NetworkStream);
        }

        public void Start(bool sendPings)
        {
            if (!this.IsRunning)
            {
                this.IsRunning = true;

                TaskContainer.Run(this.Receiver);
                TaskContainer.Run(this.Sender);
                TaskContainer.Run(this.Handler);

                if (sendPings)
                {
                    TaskContainer.Run(this.PingSender);
                }

                this.Controller.OnConnectionStart(this.Id);
            }
        }

        public void Stop()
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

                this.Controller.OnConnectionStop(this.Id);
            }
        }

        public void Send(object message)
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

                    var message = JsonConvert.DeserializeObject<object>(data, this.SerializerSettings);

                    lock (this.Input)
                    {
                        this.Input.Add(message);
                    }

                    this.Controller.DownTraffic += data.Length;
                    this.Controller.OnConnectionReceivedMessage(this.Id, data);
                    Thread.Sleep(this.SleepTime);
                }
                catch (IOException)
                {
                    this.Stop();
                }
                catch (SocketException)
                {
                    this.Stop();
                }
                catch (Exception e)
                {
                    this.Controller.OnUnhandledException(e);
                    this.Stop();
                }
            }
        }

        private void Sender()
        {
            while (this.Controller.IsRunning && this.IsRunning)
            {
                try
                {
                    var message = default(object);

                    lock (this.Output)
                    {
                        message = this.Output.ElementAtOrDefault(0);
                    }

                    if (message is null)
                    {
                        Thread.Sleep(this.SleepTime);
                        continue;
                    }

                    lock (this.Output)
                    {
                        this.Output.RemoveAt(0);
                    }

                    var data = JsonConvert.SerializeObject(message, this.SerializerSettings);

                    this.StreamWriter.WriteLine(data);
                    this.StreamWriter.Flush();

                    this.Controller.UpTraffic += data.Length;
                    this.Controller.OnConnectionSentMessage(this.Id, data);
                    Thread.Sleep(this.SleepTime);
                }
                catch (IOException)
                {
                    this.Stop();
                }
                catch (SocketException)
                {
                    this.Stop();
                }
                catch (Exception e)
                {
                    this.Controller.OnUnhandledException(e);
                    this.Stop();
                }
            }
        }

        private void Handler()
        {
            while (this.Controller.IsRunning && this.IsRunning)
            {
                try
                {
                    var message = default(object);

                    lock (this.Input)
                    {
                        message = this.Input.ElementAtOrDefault(0);
                    }

                    if (message is null)
                    {
                        Thread.Sleep(this.SleepTime);
                        continue;
                    }

                    lock (this.Input)
                    {
                        this.Input.RemoveAt(0);
                    }

                    if (message is Ping)
                    {
                        this.Send(new Pong());
                    }
                    else if (message is Pong)
                    {
                    }
                    else
                    {
                        this.Controller.OnConnectionHandleMessage(this.Id, message);
                    }

                    Thread.Sleep(this.SleepTime);
                }
                catch (Exception e)
                {
                    this.Controller.OnUnhandledException(e);
                }
            }
        }

        private void PingSender()
        {
            var stopwatch = Stopwatch.StartNew();

            while (this.Controller.IsRunning && this.IsRunning)
            {
                if (TimeSpan.FromMinutes(1) <= stopwatch.Elapsed)
                {
                    this.Send(new Ping());
                    stopwatch.Restart();
                }

                Thread.Sleep(this.SleepTime);
            }
        }
    }
}