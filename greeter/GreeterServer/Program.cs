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
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;

namespace GreeterServer
{
	class GreeterImpl : Greeter.GreeterBase
	{
		string instanceName;
        
		public GreeterImpl(string instanceName)
		{
			this.instanceName = instanceName;
		}
		// Server side handler of the SayHello RPC
		public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
		{
			var greetHeader = string.Join(", ", context.RequestHeaders.Select(header => $"[{header.Key}:{header.Value}]"));
			Console.WriteLine($"got a request from {context.Host} with {greetHeader}");
			return Task.FromResult(new HelloReply { Message = $"[{this.instanceName}] Hello {request.Name}" });
		}
	}

	class Program
	{
		const int DefaultPort = 3000;

		public static void Main(string[] args)
		{
			int port;
			if (!int.TryParse(Environment.GetEnvironmentVariable("GREETINGS_PORT"), out port))
			{
				port = DefaultPort;
			}
            var instanceName = Environment.GetEnvironmentVariable("GREETINGS_NAME") ?? Environment.MachineName;
 
			Server server = new Server
			{
				Services = { Greeter.BindService(new GreeterImpl(instanceName)) },
				Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
			};
			server.Start();

			Console.WriteLine($"Greeter server [{instanceName}] listening on port [{port}]");

			var syncTask = new TaskCompletionSource<bool>();
			
			System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += (context) => {
				Console.WriteLine("Greeter server received kill signal...");
				server.ShutdownAsync().Wait();
				syncTask.SetResult(true);
			};

			syncTask.Task.Wait(-1);
			Console.WriteLine("Greeter server stopped");
		}
	}
}
