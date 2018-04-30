namespace RemoteTest
{
    using System;
    using System.Threading.Tasks;

    using Remote;

    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
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

                        Console.WriteLine("Server : " + t.Count);

                        // we have our test object. 
                        t.Count++;
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

            // dispose of the server
            server.Dispose();

            // wait for the client to exit. 
            client.Wait();
        }

        /// <summary>
        /// Creates the client.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        /// <returns></returns>
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
    }

    public class Test
    {
        public int Count { get; set; } = 1;
    }
}
