using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace program
{
    class Main_class
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 4098;
        private const int PORT = 8000;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];

        private static Socket ClientSocket = new Socket
           (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static byte[] client_buffer = new byte[BUFFER_SIZE]; 

        static void Main()
        {
            Console.Title = "CC - ConsoleChat";
            Console.WriteLine("Wellcome to CC ConsoleChat!");
            Console.WriteLine("Turn off any firewall!!!");
            Console.WriteLine("H/J(q/w) H-host J-join");
            string inp = Console.ReadLine();
            inp = inp.ToLower();
            if (inp == "j" || inp == "q")
            {
                Console.Write("Type ip to connect: ");
                string ip = Console.ReadLine();
                ConnectToServer(IPAddress.Parse(ip));
                Console.Title = "CC - Client ** " + ClientSocket.LocalEndPoint;
                RequestLoop();
                Exit();
            }
            else if (inp == "h" || inp == "w")
            {
                SetupServer();
                Console.Title = "CC - Host ** " + serverSocket.LocalEndPoint;
                Console.WriteLine("Press Enter to close server...");
                Console.ReadLine();
                CloseAllSockets();
            }
        }
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("{0} connected...", socket.RemoteEndPoint);
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine("{0} # " + text, current.RemoteEndPoint);


            string date = DateTime.Now.ToLongTimeString().ToString();
            current.Send(Encoding.ASCII.GetBytes(date));

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }

        private static void ConnectToServer(IPAddress ip)
        {
            int attempts = 0;

            while (!ClientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                    ClientSocket.Connect(ip, PORT);
                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Connected");
            Console.WriteLine("Starting async listening...");
        }

        private static void RequestLoop()
        {
            Console.WriteLine(@"<Type ""exit"" to properly disconnect client>");

            while (true)
            {
                SendRequest();
                ReceiveResponse();
            }
        }

        private static void Exit()
        {
            SendString("exit"); // Tell the server we are exiting
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }

        private static void SendRequest()
        {
            Console.Write("{0} # ", ClientSocket.LocalEndPoint);
            string request = Console.ReadLine();
            SendString(request);

            if (request.ToLower() == "exit")
            {
                Exit();
            }
        }

        private static void SendString(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            ClientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void ReceiveResponse()
        {
            var buffer = new byte[BUFFER_SIZE];
            int received = ClientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = "\n" + Encoding.ASCII.GetString(data);
            Console.WriteLine(text);
        }
    }
}