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

            var server = $"{host}:{port}";
            Console.WriteLine($"Connecting to {server}");
            Channel channel = new Channel(server, ChannelCredentials.Insecure);

            var client = new Greeter.GreeterClient(channel);
            String user = "you";
            
            Console.WriteLine($"Sending greeting with deadline {timeoutMSecs} msecs");
            var headerMetadata = new Metadata();
            headerMetadata.Add("X-GREET", "bob|charlie");
            var reply = client.SayHello(new HelloRequest { Name = user }, headerMetadata, DateTime.UtcNow.AddMilliseconds(timeoutMSecs));
            Console.WriteLine("Greeting: " + reply.Message);

            channel.ShutdownAsync().Wait();
        }
    }
}
