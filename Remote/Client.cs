namespace Remote
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// The client.
    /// </summary>
    /// <seealso cref="RemoteStd.RemoteBase" />
    public sealed class Client : RemoteBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Client" /> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        public Client(string address, int port = RemoteConfig.Port)
        {
            this.Disposed = false;

            this.Socket = new TcpClient(address, port).Client;

            if (!this.Connected)
            {
                return;
            }

            this.Reader();
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Client" /> class from being created.
        /// </summary>
        private Client()
        {
            this.Socket = new TcpClient().Client;
        }

        /// <summary>
        /// Gets or sets a value indicating whether we are connected.
        /// </summary>
        public new bool Connected => this.Socket.Connected;

        /// <summary>
        /// Gets the socket.
        /// </summary>
        /// <value>
        /// The socket.
        /// </value>
        private Socket Socket
        {
            get;
        }

        /// <summary>
        /// The wait for server.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>
        /// The Client.
        /// </returns>
        public static async Task<Client> WaitForServer(string address, int port = RemoteConfig.Port, TimeSpan? timeout = null)
        {
            var end = timeout == null ? DateTime.Now.AddYears(1) : DateTime.Now.Add((TimeSpan)timeout);
            var client = new Client();

            while (DateTime.Now < end)
            {
                if (client.Socket.ConnectAsync(address, port).Wait(750))
                {
                    client.Reader();
                    return client;
                }

                client = new Client();
                await Task.Delay(150);
            }

            return client;
        }

        /// <summary>
        /// The send.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Send(object message)
        {
            base.Send(this.Socket, message);
        }

        /// <summary>
        /// The send.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Send(string message)
        {
            base.Send(this.Socket, message);
        }

        /// <summary>
        /// The send.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Send(byte[] message)
        {
            base.Send(this.Socket, message);
        }

        /// <summary>
        /// Sends the raw.
        /// </summary>
        /// <param name="message">The message.</param>
        public void SendRaw(params byte[] message)
        {
            base.SendRaw(this.Socket, message);
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public override void Dispose()
        {
            if (this.Disposed || !this.Connected)
            {
                return;
            }

            // notify the server we are exiting. 
            this.Send(this.Socket, RemoteConfig.ClientDisconnecting);

            // close our socket.
            this.Socket.Shutdown(SocketShutdown.Both);

            base.Dispose();
        }

        /// <summary>
        /// The reader.
        /// </summary>
        private void Reader()
        {
            Task.Run(() => this.Reader(this.Socket));
        }
    }
}
