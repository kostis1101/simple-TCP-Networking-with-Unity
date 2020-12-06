using System;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace server_app
{
    class Program
    {
        static List<TcpClient> clients = new List<TcpClient>();
        static List<NetworkStream> streams = new List<NetworkStream>();
        static List<StreamReader> readers = new List<StreamReader>();
        static List<StreamWriter> writers = new List<StreamWriter>();
        static TcpListener listener = new TcpListener(System.Net.IPAddress.Any, 5050);

        static Dictionary<TcpClient, string> clientsNames = new Dictionary<TcpClient, string>();

        static List<string> playerNames = new List<string>();

        static void Main(string[] args)
        {
            listener.Start();
            Thread connection_thread = new Thread(new ThreadStart(CheckNewConnections));
            connection_thread.Start();
        }

        static void CheckNewConnections()
        {
            Console.WriteLine("waiting for a connection");
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine($"connection accepted from {client.Client.RemoteEndPoint}");
            NetworkStream stream = client.GetStream();
            StreamReader sr = new StreamReader(stream);
            StreamWriter sw = new StreamWriter(stream);

            Thread reading_thread = new Thread(() => Read(client, stream, sr, sw));
            reading_thread.Start();

            clients.Add(client);
            streams.Add(stream);
            readers.Add(sr);
            writers.Add(sw);

            CheckNewConnections();
        }

        static void Read(TcpClient c, NetworkStream s, StreamReader r, StreamWriter w)
        {
            try
            {
                byte[] buffer = new byte[1024];
                s.Read(buffer, 0, buffer.Length);
                int recv = 0;
                foreach (byte b in buffer)
                {
                    if (b != 0)
                        recv++;
                }
                if (recv <= 0)
                {
                    Disconnect(c, s, r, w);
                }
                string request = Encoding.UTF8.GetString(buffer, 0, recv);
                if (request == "")
                {
                    return;
                }
                if (request.Contains("add player"))
                {
                    string[] commands = request.Split(':');
                    string name = commands[1];
                    playerNames.Add(name); // add name in the player list namea
                    clientsNames.Add(c, name);
                    foreach(string n in playerNames)
                    {
                        if (n != name)
                        {
                            SendMessage(w, $"add player:{n}:{commands[2]}:{commands[3]}");
                        }
                    }
                }
                Console.WriteLine($"request received \"{request}\" from: {c.Client.RemoteEndPoint}, with a length of: {recv}");
                foreach (TcpClient client in clients)
                {
                    if (client != c)
                    {
                        NetworkStream stream = client.GetStream();
                        StreamWriter writer = new StreamWriter(stream);
                        writer.WriteLine(request);
                        writer.Flush();
                        Console.WriteLine($"sending data to client {client.Client.RemoteEndPoint}");
                    }
                }
                Read(c, s, r, w);
            }
            catch
            {
                Disconnect(c, s, r, w);                
            }
        }

        private static void Disconnect(TcpClient c, NetworkStream s, StreamReader r, StreamWriter w)
        {
            // TODO Disconnect
            Console.WriteLine("something went wrong");
            clients.Remove(c);

            Console.WriteLine("someone disconnected");

            // Disconnect ("disconnect:<player name>")
            foreach (TcpClient client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.WriteLine($"disconnect:{clientsNames[c]}");
                    writer.Flush();
                    clientsNames.Remove(c);
                }
                catch { }
            }

            // close all the nessary stuff so that the player can desconnect again
            r.Close();
            w.Close();
            s.Close();
            c.Close();
        }

        static void SendMessage(StreamWriter sw, string message)
        {
            sw.WriteLine(message);
            sw.Flush();
        }
    }
}
