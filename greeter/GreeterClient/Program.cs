// Copyright 2015, Google Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;

namespace GreeterClient
{
    class Program
    {
        public static void Main(string[] args)
        {
            var instanceName = Environment.GetEnvironmentVariable("GREETINGS_NAME") ?? Environment.MachineName;
            
            var host = Environment.GetEnvironmentVariable("GREETINGS_HOST") ?? "localhost";
            int port;
            if (!int.TryParse(Environment.GetEnvironmentVariable("GREETINGS_PORT"), out port))
            {
                port = 3000;
            }

            double timeoutMSecs;
            if (!double.TryParse(Environment.GetEnvironmentVariable("GREETINGS_TIMEOUT_MSECS"), out timeoutMSecs))
            {
                timeoutMSecs = 1000 * 30;
            }

            int delay;
            if (!int.TryParse(Environment.GetEnvironmentVariable("GREETINGS_DELAYMS"), out delay))
            {
                delay = 0;
            }

            int numTasks;
            if (!int.TryParse(Environment.GetEnvironmentVariable("GREETINGS_NUMTASKS"), out numTasks))
            {
                numTasks = 1;
            }

            int numBytes;
            string payload = string.Empty;
            if (int.TryParse(Environment.GetEnvironmentVariable("GREETINGS_NUMBYTES"), out numBytes))
            {
                if (numBytes > 0)
                {
                    payload = new string('*', numBytes);
                }
            }

            var server = $"{host}:{port}";
            Console.WriteLine($"Connecting to {server}");
            Console.WriteLine($"Sending greetings at delay {delay}ms from {numTasks} tasks with deadline {timeoutMSecs}ms");

            Channel channel = new Channel(server, ChannelCredentials.Insecure);
            var client = new Greeter.GreeterClient(channel);           
            var headerMetadata = new Metadata();
            headerMetadata.Add("X-GREET", instanceName);

            var tasks = new List<Task>();
            for(int i = 0; i < numTasks; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() => {
                    while(true)
                    {
                        var sw = Stopwatch.StartNew();
                        var unaryCall = client.SayHelloAsync(new HelloRequest { Name = payload }, headerMetadata, DateTime.UtcNow.AddMilliseconds(timeoutMSecs));
                        var recvTask = unaryCall.ResponseAsync;
                        var headerTask = unaryCall.ResponseHeadersAsync;
                        
                        Task.WaitAll(new Task[] {recvTask, headerTask});
                        var replyHeaders = headerTask.Result;
                        var reply = recvTask.Result;

                        var elapsed = sw.ElapsedMilliseconds;
                        var helloFrom = string.Join(",", replyHeaders
                            .Where(header => string.Equals(header.Key, "X-GREET", StringComparison.OrdinalIgnoreCase))
                            .Select(header => $"{header.Value}"));
                        Console.WriteLine($"[{taskId}] OK [{numBytes}-{reply.CalculateSize()}] in [{elapsed}]ms from {helloFrom}");

                        if (delay <= 0)
                        {
                            break;
                        }

                        Task.Delay(delay).Wait();
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Shutting down");

            channel.ShutdownAsync().Wait();
        }
    }
}
