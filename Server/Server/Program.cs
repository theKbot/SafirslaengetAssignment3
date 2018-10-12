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
        static readonly String[] allowedMethods = { "create", "read", "update", "delete", "echo" };
        static void Main(string[] args)
        {
            //Create data object
            Category[] data = CreateData();

            //server
            var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            server.Start();
            Console.WriteLine("Server started ..");

            while(true)
            {
                Console.WriteLine("Waiting for client");
                var client = server.AcceptTcpClient();
                Console.WriteLine("Client found! :D");
                var strm = client.GetStream();
                var buffer = new Byte[client.ReceiveBufferSize];
                try
                {
                    var readCnt = strm.Read(buffer, 0, buffer.Length);
                    var msg = Encoding.UTF8.GetString(buffer, 0, readCnt);
                    Request req = JsonConvert.DeserializeObject<Request>(msg);
                    Response res = CheckConstraints(req, client);
                    if (res.Status != null)
                    {
                        client.SendResponse(res);
                    }
                }
                catch (IOException) { }
                catch (NullReferenceException)
                {
                    Response r = new Response()
                    {
                        Status = "4 Missing method"
                    };
                    client.SendResponse(r);
                }
            } 
        }

        static void HandleRequest(TcpClient client)
        {
            var strm = client.GetStream();
            var buffer = new Byte[client.ReceiveBufferSize];
            //Catch fejl, og send en response tilbage
            try
            {
                Console.WriteLine("Trying");
                var readCnt = strm.Read(buffer, 0, buffer.Length);
                var msg = Encoding.UTF8.GetString(buffer, 0, readCnt);
                Request req = JsonConvert.DeserializeObject<Request>(msg);
                Response res = CheckConstraints(req, client);
                

                if(res.Status != null)
                {
                    client.SendResponse(res);
                    return;
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("catch null error");
                Response r = new Response()
                {
                    Status = "Missing method"
                };
                client.SendResponse(r);
            }
            catch(Exception e)
            {
                Console.WriteLine("catch: "+e);
                return;
            }
        }

        static Response CheckConstraints(Request req, TcpClient client)
        {
            Response res = new Response();
                /*  CONSTRAINTS  */
            //Missing method
            if(req.Method == null)
            {
                AddToStatus(res, "Missing method!");
            }
            else 
            {
                //Illegal method
                //TODO: This is shit code!!! FIX
                foreach (string a in allowedMethods)
                {
                    if (req.Method == a)
                    {
                        goto Match;
                    }
                }
                AddToStatus(res, "Illegal Method");
                
            Match:
                Console.WriteLine();
            }

            //Missing resource
            if (req.Path == null && req.Method != "echo")
            {
                AddToStatus(res, "Missing resource");
            }
            //Vi skal kun kigge på der er en path, men vi skal ikke tjekke om det er en lovlig path, det er først 
            //når vi udfører requesten at vi skal tjekke path ???
            /*
            {
                if (!File.Exists(req.Path))
                {
                    //bad request
                    res.Status = "4 Bad Request";
                    Console.WriteLine("ip");
                    return res;
                }
            }
            */
            //Missing date
            if(req.Date == null)
            {
                AddToStatus(res, "Missing date");
            }
            else
            {
                if (!int.TryParse(req.Date, out int e))
                {
                    AddToStatus(res, "Illegal date");
                }
            }

            //Missing body
            if (req.Body == null && (req.Method == "create" || req.Method == "update" || req.Method == "echo"))
            {
                AddToStatus(res, "Missing body");
            }
            else
            {
                try
                {
                    var tmpJson = JObject.Parse(req.Body);
                }
                catch(Exception)
                {
                    AddToStatus(res, "Illegal body");
                }
            }
            
            //Missing unix time
            if (!int.TryParse(req.Date, out int e_m))
            {
                AddToStatus(res, "Illegal date");
            }
            if(res.Status != null)
            {
                res.Status = res.Status.Insert(0, "4 ");
            }

            //Echo "Hello world
            if (req.Method == "echo")
            {
                string message = req.Body;
                AddToBody(res, "Hello World");
                client.SendResponse(res);
            }
            return res;
        }

     
        //This funtion adds a message to the Response object's status
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

        static void AddToBody(Response r, string message)
        {
            if (r.Body == null)
            {
                r.Body += message;
            }
            else
            {
                r.Body += ", " + message;
            }
        }


        //For converting the datetime to unix
        private static string UnixTimestamp()
        {
            return DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        }

        //This function creates the data used
        static Category[] CreateData()
        {
            var d = new Category[]
            {
                new Category{Cid = "1", Name = "Beverages"},
                new Category{Cid = "2", Name = "Condiments"},
                new Category{Cid = "3", Name = "Confections"}
            };
            //Returning the D
            return d;
        }
    }

    static class Util
    {
        public static void SendResponse(this TcpClient client, Response r)
        {
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(r));
            client.GetStream().Write(payload, 0, payload.Length);
            client.GetStream().Close();
            client.Close();
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
        public string Cid { get; set; }
        public string Name { get; set; }
    }
}
