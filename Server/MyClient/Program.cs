using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new TcpClient();

            client.Connect(IPAddress.Parse("127.0.0.1"), 5000);

            var jsonobject = new
            {
                method = "read",
                path = "/api/categories/1",
                date = DateTimeOffset.Now.ToUnixTimeSeconds().ToString()
            };

            var strm = client.GetStream();

            var messageAsJson = JsonConvert.SerializeObject(jsonobject);

            //var obj = JObject.Parse(messageAsJson);

            var msg = Encoding.UTF8.GetBytes(messageAsJson);

            strm.Write(msg, 0, msg.Length);

            var buffer = new byte[client.ReceiveBufferSize];

            var readCnt = strm.Read(buffer, 0, buffer.Length);

            var svrMsg = Encoding.UTF8.GetString(buffer, 0, readCnt);

            Console.WriteLine($"Response: {svrMsg}");

            strm.Close();

            client.Close();
        }
    }
}
