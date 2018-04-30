namespace Remote
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// The base remote class.
    /// </summary>
    public abstract class RemoteBase : IDisposable
    {
        /// <summary>
        /// The message type index.
        /// </summary>
        private const int MessageTypeIndex = 0;

        /// <summary>
        /// The full packet size offset.
        /// </summary>
        private const int FullPacketSizeOffset = 1;

        /// <summary>
        /// The payload length size.
        /// </summary>
        private const int PayloadLengthSize = sizeof(ushort);

        /// <summary>
        /// The data index.
        /// </summary>
        private const int DataIndex = 1;

        /// <summary>
        /// The message received.
        /// </summary>
        public event RemoteMessage MessageReceived;

        /// <summary>
        /// The Raw message received.
        /// </summary>
        public event RemoteRawMessage RawMessage;

        /// <summary>
        /// Gets or sets a value indicating whether disposed.
        /// </summary>
        public bool Disposed
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether we are connected.
        /// </summary>
        public bool Connected
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether raw mode.
        /// </summary>
        public bool RawMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        private string Name => this.GetType().Name;

        /// <summary>
        /// The dispose.
        /// </summary>
        public virtual void Dispose()
        {
            this.Disposed = true;
        }

        /// <summary>
        /// The send raw.
        /// </summary>
        /// <param name="socket">
        /// The socket.
        /// </param>
        /// <param name="packet">
        /// The packet.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        protected virtual bool SendRaw(Socket socket, IList<byte> packet)
        {
            var result = socket.Send(packet.ToArray());

            return result == packet.Count;
        }

        /// <summary>
        /// The send object.
        /// </summary>
        /// <param name="socket">
        /// The socket.
        /// </param>
        /// <param name="data">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        protected virtual bool Send(Socket socket, object data)
        {
            if (!socket.Connected || data == null)
            {
                // we are not connected
                return false;
            }

            // convert the data to a byte array.
            var message = data.ToByteArray();

            // create a packet with Object as the type.
            var packet = new List<byte> { (byte)RemoteConfig.MessageTypes.Object };

            // add the size of the payload.
            packet.AddRange(BitConverter.GetBytes((ushort)(4 + message.Length)));

            // add the object.
            packet.AddRange(message);

            // send the packet.
            return this.SendRaw(socket, packet);
        }

        /// <summary>
        /// The send raw.
        /// </summary>
        /// <param name="socket">
        /// The socket.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        protected virtual bool Send(Socket socket, IList<byte> message)
        {
            if (!socket.Connected)
            {
                // socket is not connected.
                return false;
            }

            // create a new packet with the type as the first byte.
            var packet = new List<byte> { (byte)RemoteConfig.MessageTypes.Bytes };

            // add the size of the payload.
            packet.AddRange(BitConverter.GetBytes((ushort)message.Count));

            // copy the message to a packet.
            packet.AddRange(message);

            // send the packet.
            return this.SendRaw(socket, packet);
        }

        /// <summary>
        /// The send message.
        /// </summary>
        /// <param name="socket">
        /// The socket.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        protected virtual bool Send(Socket socket, string message)
        {
            if (!socket.Connected)
            {
                return false;
            }

            // create a new packet with the type as the first byte.
            var packet = new List<byte> { (byte)RemoteConfig.MessageTypes.String };

            // add the size of the payload.
            packet.AddRange(BitConverter.GetBytes((ushort)message.Length));

            // copy the message to a packet.
            packet.AddRange(Encoding.ASCII.GetBytes(message));

            // send the packet.
            return this.SendRaw(socket, packet);
        }

        /// <summary>
        /// The socket reader.
        /// </summary>
        /// <param name="socket">
        /// The socket.
        /// </param>
        /// <param name="bufferSize">
        /// The buffer size.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        protected async Task Reader(Socket socket, int bufferSize = RemoteConfig.PacketSize)
        {
            // create a buffer to read our data into.
            var buffer = new byte[bufferSize];

            while (socket.Connected && !this.Disposed)
            {
                if (socket.Available < 1)
                {
                    await Task.Delay(10);
                }

                try
                {
                    if (this.RawMode)
                    {
                        var read = socket.Receive(buffer);
                        this.OnRawMessage(socket, buffer.Take(read).ToArray());
                    }
                    else
                    {  
                        // read the first two bytes to determine size. 
                        var read = socket.Receive(buffer, 0, FullPacketSizeOffset + PayloadLengthSize, SocketFlags.None);

                        if (read > 0)
                        {
                            // store the total size of the packet.
                            var size = BitConverter.ToUInt16(buffer, FullPacketSizeOffset);

                            // read the remainder of the packet. 
                            read = socket.Receive(buffer, DataIndex, size, SocketFlags.None) + FullPacketSizeOffset;

                            // parse the packet read from the socket.
                            this.ProcessBuffer(socket, buffer.Take(read).ToArray());
                        }
                    }
                }
                catch (Exception ex)
                {
                    // some exception occured so lets assume the socket disconnected.
                }
            }
        }

        /// <summary>
        /// The process buffer.
        /// </summary>
        /// <param name="socket">
        /// The socket.
        /// </param>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        protected virtual void ProcessBuffer(Socket socket, byte[] buffer)
        {
            if (this.RawMode)
            {
                this.OnRawMessage(socket, buffer);
                return;
            }

            if (buffer.Length < 2)
            {
                return;
            }

            switch ((RemoteConfig.MessageTypes)buffer[MessageTypeIndex])
            {
                default:
                    // invalid or bad packet.
                    return;

                case RemoteConfig.MessageTypes.String:
                    var message = Encoding.ASCII.GetString(buffer, DataIndex, buffer.Length - DataIndex);

                    if (message.Equals(RemoteConfig.ClientDisconnecting) || message.Equals(RemoteConfig.ServerExiting))
                    {
                        // the socket is trying to Disconnect.
                        socket.Shutdown(SocketShutdown.Both);
                    }

                    this.OnMessageReceived(socket, message);
                    break;

                case RemoteConfig.MessageTypes.Bytes:
                    // take the bytes from the packet. 
                    this.OnMessageReceived(socket, buffer.Skip(DataIndex).ToArray());
                    break;

                case RemoteConfig.MessageTypes.Object:

                    // convert the raw data into the object.
                    this.OnMessageReceived(socket, buffer.Skip(DataIndex).ToObject());
                    break;
            }
        }

        /// <summary>
        /// The on message received.
        /// </summary>
        /// <param name="socket">
        /// The socket.
        /// </param>
        /// <param name="packet">
        /// The object to send to the callback.
        /// </param>
        protected virtual void OnMessageReceived(Socket socket, object packet)
        {
            if (packet != null)
            {
                this.MessageReceived?.Invoke(socket, packet);
            }
        }

        /// <summary>
        /// The on raw message.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="packet">The packet buffer.</param>
        protected void OnRawMessage(Socket socket, byte[] packet)
        {
            if (packet?.Length > 0)
            {
                this.RawMessage?.Invoke(socket, packet);
            }
        }
    }
}