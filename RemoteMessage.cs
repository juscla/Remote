namespace Remote
{
    /// <summary>
    /// The remote message.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="data">
    /// The data.
    /// </param>
    public delegate void RemoteMessage(System.Net.Sockets.Socket sender, object data);

    /// <summary>
    /// Remote Raw Message
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="data">The data.</param>
    public delegate void RemoteRawMessage(System.Net.Sockets.Socket sender, byte[] data);
}