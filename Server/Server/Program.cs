using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Server
{
    class Server
    {
        static String[] allowedMethods = { "create", "read", "update", "delete", "echo" };
        static void Main(string[] args)
        {
            //server
            var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            server.Start();
            Console.WriteLine("Server started ..");

            while(true)
            {
                var client = server.AcceptTcpClient();

                Console.WriteLine("Client found! :D");

                HandleRequest(client);
            } 
        }

        static void HandleRequest(TcpClient client)
        {
            var strm = client.GetStream();
            var buffer = new Byte[client.ReceiveBufferSize];
            //Only process request if any (constraint)
            if (!strm.DataAvailable)
            {
                Console.WriteLine("OOOPS MOFO");
                return;
            }
            var readCnt = strm.Read(buffer, 0, buffer.Length);
            var msg = Encoding.UTF8.GetString(buffer, 0, readCnt);
            
            Request r = JsonConvert.DeserializeObject<Request>(msg);
            Response res = CheckConstraints(r);
            
        }

        static Response CheckConstraints(Request r)
        {
            Response res = new Response();
                /*  CONSTRAINTS  */
            //Missing method
            if(r.Method == null)
            {
                AddToStatus(res, "Missing method!");
            }
            else
            {
                foreach (string a in allowedMethods)
                {
                    if (r.Method != a)
                    {
                        AddToStatus(res, "Illegal Method");
                    }
                }
            }
            return null;
        }
        static void CreateStream(TcpClient client)
        {

        }

        static void AddToStatus(Response r, string message)
        {
            if (r.Status == null)
            {
                r.Status += message;
            }
            else
            {
                r.Status += ", " + message;
            }
        }
    }

    class Request
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Date { get; set; }
        public string Body { get; set; }
    }
    class Response
    {
        public string Status { get; set; }
        public string Body { get; set; }
    }
    class Category
    {
        string Cid { get; set; }
        string Name { get; set; }
    }
}
