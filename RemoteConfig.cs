namespace RemoteStd
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The remote config.
    /// </summary>
    public class RemoteConfig
    {
        /// <summary>
        /// The packet size.
        /// </summary>
        public const int PacketSize = 1024 * 1024 * 30;

        /// <summary>
        /// The port.
        /// </summary>
        public const int Port = 888;

        /// <summary>
        /// The client disconnecting.
        /// </summary>
        public const string ClientDisconnecting = "ClientDone";

        /// <summary>
        /// The server exiting.
        /// </summary>
        public const string ServerExiting = "ServerDone";

        /// <summary>
        /// The message types.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "Reviewed. Suppression is OK here.")]
        public enum MessageTypes
        {
            String,
            Bytes,
            Object
        }
    }
}