using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            String[] allowedMethods = { "create", "read", "update", "delete", "echo" };

            //server
            var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            server.Start();
            Console.WriteLine("Server started ..");

            while(true)
            {
                var client = server.AcceptTcpClient();
                Thread thread = new Thread(new ThreadStart(CreateStream));
                thread.Start();
            }
        }

        static void CreateStream()
        {

        }
    }

    class Request
    {
        string Method { get; set; }
        string Path { get; set; }
        string Date { get; set; }
        string Body { get; set; }
    }
    class Response
    {
        string Status { get; set; }
        string Body { get; set; }
    }
    class Category
    {
        string Cid { get; set; }
        string Name { get; set; }
    }
}
