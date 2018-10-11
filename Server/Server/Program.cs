using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                //Illegal method
                foreach (string a in allowedMethods)
                {
                    if (r.Method != a)
                    {
                        AddToStatus(res, "Illegal Method");
                    }
                }
            }

            //Missing resource
            if (r.Path == null && r.Method != "echo")
            {
                AddToStatus(res, "Missing resource");
            }
            else
            {
                if (!File.Exists(r.Path))
                {
                    AddToStatus(res, "Invaild Path");
                }
            }

            //Missing date
            if(r.Date == null)
            {
                AddToStatus(res, "Missing date");
            }
            else
            {
                if (!int.TryParse(r.Date, out int e))
                {
                    AddToStatus(res, "Illegal date");
                }
            }

            //Missing body
            if (r.Body == null && (r.Method == "create" || r.Method == "update" || r.Method == "Echo"))
            {
                AddToStatus(res, "Missing body");
            }
            else
            {
                try
                {
                    var tmpJson = JObject.Parse(r.Body);
                }
                catch(Exception e)
                {
                    AddToStatus(res, "Illegal body");
                }
            }
            
            if(res.Status == null)
            {

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
