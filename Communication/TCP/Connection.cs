﻿using SVN.Debug;
using SVN.Network.Properties;
using SVN.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace SVN.Network.Communication.TCP
{
    internal class Connection : IDisposable
    {
        private static int Indexer { get; set; }
        public int Id { get; } = ++Connection.Indexer;
        public bool IsRunning { get; private set; }
        private Controller Controller { get; }
        private TcpClient TcpClient { get; }
        private NetworkStream NetworkStream { get; }
        private StreamReader StreamReader { get; }
        private StreamWriter StreamWriter { get; }
        private Action<int, string> Handle { get; }
        private List<string> Input { get; } = new List<string>();
        private List<string> Output { get; } = new List<string>();

        public Connection(Controller controller, TcpClient tcpClient, Action<int, string> handle)
        {
            this.IsRunning = true;
            this.Controller = controller;
            this.TcpClient = tcpClient;
            this.NetworkStream = this.TcpClient.GetStream();
            this.StreamReader = new StreamReader(this.NetworkStream);
            this.StreamWriter = new StreamWriter(this.NetworkStream);
            this.Handle = handle;

            TaskContainer.Run(this.Receiver);
            TaskContainer.Run(this.Sender);
            TaskContainer.Run(this.Handler);
        }

        public void Dispose()
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
            this.Controller.Stop(this);
        }

        private TimeSpan ThreadSleeptime
        {
            get => TimeSpan.FromMilliseconds(int.Parse(Settings.ThreadSleeptime));
        }

        public void SendObject(string message)
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
                    var message = this.StreamReader.ReadLine();

                    if (message is null)
                    {
                        continue;
                    }

                    lock (this.Input)
                    {
                        this.Input.Add(message);
                    }

                    Thread.Sleep(this.ThreadSleeptime);
                }
                catch (SocketException)
                {
                    this.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Write($"exception in Connection.Receiver: {e}");
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
                    if (this.Output.Any())
                    {
                        var message = default(string);

                        lock (this.Output)
                        {
                            message = this.Output.ElementAt(0);
                            this.Output.RemoveAt(0);
                        }

                        if (message is null)
                        {
                            continue;
                        }

                        this.StreamWriter.WriteLine(message);
                        this.StreamWriter.Flush();
                    }

                    Thread.Sleep(this.ThreadSleeptime);
                }
                catch (SocketException)
                {
                    this.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Write($"exception in Connection.Sender: {e}");
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
                    if (this.Input.Any())
                    {
                        var message = default(string);

                        lock (this.Input)
                        {
                            message = this.Input.ElementAt(0);
                            this.Input.RemoveAt(0);
                        }

                        if (message is null)
                        {
                            continue;
                        }

                        this.Handle(this.Id, message);
                    }

                    Thread.Sleep(this.ThreadSleeptime);
                }
                catch (Exception e)
                {
                    Logger.Write($"exception in Connection.Handler: {e}");
                    this.Dispose();
                }
            }
        }
    }
}