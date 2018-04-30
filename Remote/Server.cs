namespace Remote
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// The server.
    /// </summary>
    /// <seealso cref="RemoteStd.RemoteBase" />
    public class Server : RemoteBase
    {
        /// <summary>
        /// The listener.
        /// </summary>
        private readonly TcpListener listener;

        /// <summary>
        /// The run.
        /// </summary>
        private bool run;

        /// <summary>
        /// Initializes a new instance of the <see cref="Server" /> class.
        /// </summary>
        /// <param name="port">The port.</param>
        public Server(int port = RemoteConfig.Port)
        {
            this.listener = new TcpListener(IPAddress.Any, port);
        }

        /// <summary>
        /// The client removed.
        /// </summary>
        public event EventHandler<Socket> ClientRemoved;

        /// <summary>
        /// The client Added.
        /// </summary>
        public event EventHandler<Socket> ClientAdded;

        /// <summary>
        /// Gets the address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public string Address => this.listener.LocalEndpoint.ToString();

        /// <summary>
        /// Gets the clients.
        /// </summary>
        /// <value>
        /// The clients.
        /// </value>
        public List<Socket> Clients { get; } = new List<Socket>(10);

        /// <summary>
        /// The start new.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>
        /// The <see cref="Server" />.
        /// </returns>
        public static Server StartNew(int port = RemoteConfig.Port)
        {
            var server = new Server(port);

            server.Start();

            return server;
        }

        /// <summary>
        /// The start.
        /// </summary>
        public void Start()
        {
            if (this.run)
            {
                return;
            }

            this.Disposed = false;

            this.run = true;

            this.listener.Start();

            Task.Run(() => this.AwaitClients());
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public override void Dispose()
        {
            this.run = false;

            this.NotifyClients(RemoteConfig.ServerExiting);

            lock (this.Clients)
            {
                this.Clients.Clear();
            }

            base.Dispose();
        }

        /// <summary>
        /// The notify clients.
        /// </summary>
        /// <param name="message">The message.</param>
        public void NotifyClients(string message)
        {
            lock (this.Clients)
            {
                this.Clients.ForEach(c => this.NotifyClient(c, message));
            }
        }

        /// <summary>
        /// The notify clients.
        /// </summary>
        /// <param name="message">The message.</param>
        public void NotifyClients(object message)
        {
            lock (this.Clients)
            {
                this.Clients.ForEach(c => this.NotifyClient(c, message));
            }
        }

        /// <summary>
        /// The Raw notify clients.
        /// </summary>
        /// <param name="message">The message.</param>
        public void RawNotifyClients(byte[] message)
        {
            lock (this.Clients)
            {
                this.Clients.ForEach(c => this.RawNotifyClient(c, message));
            }
        }

        /// <summary>
        /// The notify clients.
        /// </summary>
        /// <param name="message">The message.</param>
        public void NotifyClients(byte[] message)
        {
            lock (this.Clients)
            {
                this.Clients.ForEach(c => this.NotifyClient(c, message));
            }
        }

        /// <summary>
        /// The notify client.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="message">The message.</param>
        public void NotifyClient(Socket client, string message)
        {
            this.Send(client, message);
        }

        /// <summary>
        /// The notify client.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="message">The message.</param>
        public void NotifyClient(Socket client, object message)
        {
            this.Send(client, message);
        }

        /// <summary>
        /// The notify client.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="message">The message.</param>
        public void NotifyClient(Socket client, byte[] message)
        {
            this.Send(client, message);
        }

        /// <summary>
        /// The raw notify client.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void RawNotifyClient(Socket client, byte[] message)
        {
            Console.WriteLine($"Sending {message.Length}");
            this.SendRaw(client, message);
        }

        /// <summary>
        /// The listen for clients.
        /// </summary>
        private async void AwaitClients()
        {
            while (this.run)
            {
                if (!this.listener.Pending())
                {
                    await Task.Delay(250);
                    continue;
                }

                // accept the client
                var socket = await this.listener.AcceptTcpClientAsync();
                var client = socket.Client;

                lock (this.Clients)
                {
                    // add the client
                    this.Clients.Add(client);
                }

                this.OnClientAdded(client);

                // read the client.
                Task.Run(() => this.ClientReader(client));
            }

            this.listener.Stop();
        }

        /// <summary>
        /// The Client Reader Method.
        /// </summary>
        /// <param name="client">The client to read from.</param>
        private async void ClientReader(Socket client)
        {
            // reads the socket
            await this.Reader(client);

            lock (this.Clients)
            {
                // remove the client
                this.Clients.Remove(client);
            }

            this.OnClientRemoved(client);
        }

        /// <summary>
        /// The on client removed.
        /// </summary>
        /// <param name="socket">The socket.</param>
        private void OnClientRemoved(Socket socket)
        {
            this.ClientRemoved?.Invoke(this, socket);
        }

        /// <summary>
        /// The on client removed.
        /// </summary>
        /// <param name="socket">The socket.</param>
        private void OnClientAdded(Socket socket)
        {
            this.ClientAdded?.Invoke(this, socket);
        }
    }
}
