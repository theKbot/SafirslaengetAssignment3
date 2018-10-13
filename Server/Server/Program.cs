using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Server
{
    class Server
    {
        //Create data object
        static List<Category> Data = CreateData();
        static readonly String[] allowedMethods = { "create", "read", "update", "delete", "echo" };
        static TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
        static void Main(string[] args)
        {
            //server
                        server.Start();
            Console.WriteLine("Server started ..");
            StartServerLoop();
        }

        static async Task StartServerLoop()
        {
            while (true)
            {
                Console.WriteLine("Waiting for client");
                var client = server.AcceptTcpClient();
                await CreateThread(client);
            }
        }

        static async Task CreateThread(TcpClient client)
        {
            Response res = new Response();
            var strm = client.GetStream();
            var buffer = new Byte[client.ReceiveBufferSize];
            try
            {
                var readCnt = strm.Read(buffer, 0, buffer.Length);
                var msg = Encoding.UTF8.GetString(buffer, 0, readCnt);
                Request req = JsonConvert.DeserializeObject<Request>(msg);
                res = CheckConstraints(req, client);
                if (res.Status != null)
                {
                    client.SendResponse(res);
                }
                else
                {
                    res = HandleRequest(req, client);
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
                bool hasMethod = false;
                foreach (string a in allowedMethods)
                {
                    if (req.Method == a)
                    {
                        hasMethod = true;
                    }
                }
                if (!hasMethod)
                {
                    AddToStatus(res, "Illegal Method");
                }
            }

            //Missing resource
            if (req.Path == null && req.Method != "echo")
            {
                AddToStatus(res, "Missing resource");
            }

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
            else if(req.Method == "create" || req.Method == "update")
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
            if (req.Method == "echo" && req.Body != null)
            {
                var message = req.Body;
                AddToBody(res, message);
                res.Status = "1 Ok";
            }
            return res;
        }

        static Response HandleRequest(Request req, TcpClient client)
        {
            Response res = new Response();

            //Create a new object
            if (req.Method == "create")
            {
                if (req.Path == "/api/categories")
                {
                    try
                    {
                        Category c = JsonConvert.DeserializeObject<Category>(req.Body);

                        int highest = 0;
                        foreach (Category cc in Data)
                        {
                            if (cc.cid > highest)
                            {
                                highest = cc.cid;
                            }
                        }
                        highest += 1;
                        c.cid = highest;
                        Data.Add(c);
                        AddToStatus(res, "2 Created");
                        AddToBody(res, JsonConvert.SerializeObject(c));
                    }
                    catch
                    {
                        AddToStatus(res, "4 Bad Request");
                    }
                }
                else
                {
                    AddToStatus(res, "4 Bad Request");
                }
            } // end of create

            //update
            if(req.Method == "read")
            {
                string[] path = req.Path.Split('/');
                if(path[0].Length == 0 && path[1] == "api" && path[2] == "categories")
                {
                    //Return alt fra "databasen"
                    if(path.Length == 3)
                    {
                        AddToStatus(res, "1 Ok");
                        
                        AddToBody(res, JsonConvert.SerializeObject(Data));
                    }
                    else if(path.Length == 4)
                    {
                        if(int.TryParse(path[3], out int id))
                        {
                            bool hasFound = false;
                            foreach (Category c in Data)
                            {
                                if(c.cid == id)
                                {
                                    //Match found, return match
                                    AddToStatus(res, "1 Ok");
                                    AddToBody(res, JsonConvert.SerializeObject(c));
                                    hasFound = true;
                                    break;
                                }
                            }
                            if (hasFound == false)
                            {
                                //no match found return bad request
                                AddToStatus(res, "5 Not Found");
                            }
                        }
                        else
                        {
                            //could not parse id, return bad request
                            AddToStatus(res, "4 Bad Request");
                        }
                    }
                }
                else //Path did not match, return bad request
                {
                    AddToStatus(res, "4 Bad Request");
                }
            } // end of read

            if (req.Method == "delete")
            {
                string[] path = req.Path.Split('/');
                if (path[0].Length == 0 && path[1] == "api" && path[2] == "categories")
                {
                    if (path.Length == 4)
                    {
                        //Path structor is correct
                        if (int.TryParse(path[3], out int id))
                        {
                            bool hasFound = false;
                            foreach (Category c in Data)
                            {
                                if (c.cid == id)
                                {
                                    //Match found, delete match and return ok
                                    Data.Remove(c);
                                    AddToStatus(res, "1 Ok");
                                    hasFound = true;
                                    break;
                                }
                            }
                            if (hasFound == false)
                            {
                                //no match found return bad request
                                AddToStatus(res, "5 Not Found");
                            }
                        }
                        else
                        {
                            //could not parse id, return bad request
                            AddToStatus(res, "4 Bad Request");
                        }
                    }
                    else if(path.Length == 3)
                    {
                        //path id not set
                        AddToStatus(res, "4 Bad Request");
                    }
                }
                else
                {
                    //path is wrong
                    AddToStatus(res, "4 Bad Request");
                }
            } // end of delete
            
            if(req.Method == "update")
            {
                string[] path = req.Path.Split('/');
                if (path[0].Length == 0 && path[1] == "api" && path[2] == "categories")
                {
                    if(path.Length == 4) { 
                        //Path structor is correct
                        if (int.TryParse(path[3], out int id))
                        {
                            bool hasFound = false;
                            foreach (Category c in Data)
                            {
                                if (c.cid == id)
                                {
                                    //Match found, update match and return updated
                                    Category tmp = JsonConvert.DeserializeObject<Category>(req.Body);
                                    c.cid = tmp.cid;
                                    c.name = tmp.name;
                                    AddToStatus(res, "3 Updated");
                                    hasFound = true;
                                    break;
                                }
                            }
                            if (hasFound == false)
                            {
                                //no match found return bad request
                                AddToStatus(res, "5 Not Found");
                            }
                        }
                        else
                        {
                            //could not parse id, return bad request
                            AddToStatus(res, "4 Bad Request");
                        }
                    }
                    else if(path.Length == 3)
                    {
                        //Path id not set
                        AddToStatus(res, "4 Bad Request");
                    }
                }
                else
                {
                    //path is wrong
                    AddToStatus(res, "4 Bad Request");
                }
            } //end of update

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
        }


        //For converting the datetime to unix
        private static string UnixTimestamp()
        {
            return DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
        }

        //This function creates the data used
        static List<Category> CreateData()
        {
            var d = new List<Category>
            {
                new Category{cid = 1, name = "Beverages"},
                new Category{cid = 2, name = "Condiments"},
                new Category{cid = 3, name = "Confections"}
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
        public int cid { get; set; }
        public string name { get; set; }
    }
}
