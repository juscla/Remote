namespace RemoteTest
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Remote;

    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            // Create a server on port 888
            var server = Server.StartNew(888);

            // subscribe to events to know when a client is added and or removed. 
            server.ClientAdded += (s, e) => Console.WriteLine($"Client Added @: {e.RemoteEndPoint}");
            server.ClientRemoved += (s, e) => Console.WriteLine($"Client Removed @: {e.RemoteEndPoint}");

            // listen for messages from the clients and increment the counter.
            // then send back the object to the client.
            server.MessageReceived += (s, e) =>
            {
                switch (e)
                {
                    case Test t:

                        // we have our test object. 
                        // print the current Count.
                        Console.WriteLine("Server : " + t.Count);

                        // increment the count
                        t.Count++;

                        // send back to the client the updated object [incremented by 1].
                        server.NotifyClient(s, t);
                        break;

                    default:
                        // unknown.
                        Console.WriteLine(e);
                        break;
                }
            };

            // start our client async connection.
            // passing the server address and server port. 
            // this will run as a different thread. 
            var client = CreateClient("localhost", 888);

            // use the idispose pattern on the server. 
            using (server)
            {
                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        if (Console.ReadKey(true).Key == ConsoleKey.Q)
                        {
                            break;
                        }
                    }

                    Task.Delay(100);
                }
            }

            // wait for the client to exit. 
            client.Wait();
        }

        /// <summary>
        /// Creates the client.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        /// <returns>
        /// The task to wait for. 
        /// </returns>
        private static async Task CreateClient(string address, int port)
        {
            // create a connection client to the server,
            // try to address for up to 1 minute.
            var client = await Client.WaitForServer(address, port, TimeSpan.FromMinutes(1));

            // check if we have a valid Client object.
            if (client == null)
            {
                Console.WriteLine("Server connection Timed out");
                return;
            }

            using (client)
            {
                // callback for when we get a message from the server. 
                client.MessageReceived += (s, e) =>
                {
                    switch (e)
                    {
                        case Test t:
                            Console.WriteLine("Client : " + t.Count);

                            // delay some time 
                            Thread.Sleep(1000);

                            // increment our counter and send it back to the server
                            t.Count++;
                            client.Send(t);

                            break;
                    }
                };

                // send our initial message
                client.Send(new Test());

                while (client.Connected)
                {
                    await Task.Delay(100);
                }
            }
        }

        /// <summary>
        /// The test.
        /// </summary>
        public class Test
        {
            /// <summary>
            /// Gets or sets the count.
            /// </summary>
            /// <value>
            /// The count.
            /// </value>
            public int Count { get; set; } = 1;
        }
    }
}
