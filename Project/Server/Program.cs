﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Schema;
using ConfigStorageSP;
using Grpc.Core;
using Newtonsoft.Json.Linq;

namespace ServerSP
{

    class ServerServices : ServerService.ServerServiceBase
    {

        Dictionary<(int, int), string> dataStorage = new Dictionary<(int, int), string>();

        public ServerServices() { }

        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            string res;

            lock (this)
            {
                try
                {
                    res = dataStorage[(request.PartitionId, request.ObjectId)];
                } catch(KeyNotFoundException e)
                {
                    res = "N/A";
                }
            }

            return Task.FromResult(new ReadReply
            {
                ObjectValue = res
            });
        }

        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            lock (this)
            {
                dataStorage[(request.PartitionId, request.ObjectId)] = request.ObjectValue;
            }

            return Task.FromResult(new WriteReply { Ok = true });
        }

    }

    class Program
    {

        public static void Main(string[] args)
        {
            ConfigStorage config = new ConfigStorage("teste.json");
            JToken serverConfig = Program.getServerConfigs(config);

            Uri uri = new Uri(serverConfig["Url"].ToObject<string>());

            Server server = new Server
            {
                Services = { ServerService.BindService(new ServerServices()) },
                Ports = { new ServerPort(uri.Host, uri.Port, ServerCredentials.Insecure) }
            };

            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();


        }
        
        public static JToken getServerConfigs(ConfigStorage config)
        {
            foreach(var server in config.getServers())
            {
                if(server["Taken"].ToObject<int>() == 0)
                {
                    config.takeServer(server["Id"].ToObject<int>());
                    return server;
                }
            }
            return null;
        }
    }
}
