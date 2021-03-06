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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;

namespace GreeterServer
{
	class GreetingFederator
	{
		readonly List<Channel> downStreamChannels;
		readonly string instanceName;

		public GreetingFederator( string downStreamHosts, string instanceName)
		{
			this.instanceName = instanceName;

			downStreamChannels = new List<Channel>();
			foreach	(var downStreamHost in downStreamHosts.Split('|'))
			{
				if (!string.IsNullOrEmpty(downStreamHost))
				{
					Console.WriteLine($"Creating channel to downstream host {downStreamHost}");
					downStreamChannels.Add(new Channel(downStreamHost, ChannelCredentials.Insecure));
				}
			}
		}

		public List<AsyncUnaryCall<HelloReply>> SayHello(HelloRequest request)
		{
			if (downStreamChannels.Count == 0)
			{
				return null;
			}

            var headerMetadata = new Metadata();
            headerMetadata.Add("X-GREET", instanceName);

			var sendTasks = new List<AsyncUnaryCall<HelloReply>>();
			foreach (var channel in downStreamChannels)
			{
				var client = new Greeter.GreeterClient(channel);
				sendTasks.Add(client.SayHelloAsync(request, headerMetadata));
			}

			return sendTasks;
		}
	}
	class GreeterImpl : Greeter.GreeterBase
	{
		readonly string instanceName;
		readonly GreetingFederator federator;
        
		public GreeterImpl(string instanceName, GreetingFederator federator)
		{
			this.instanceName = instanceName;
			this.federator = federator;
		}

		// Server side handler of the SayHello RPC
		public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
		{
			// print who called us
			var greetHeader = string.Join(", ", context.RequestHeaders
				.Where(header => string.Equals(header.Key, "X-GREET", StringComparison.OrdinalIgnoreCase))
				.Select(header => $"[{header.Value}]"));
			Console.WriteLine($"got a request from {context.Peer} with {greetHeader}");

			// federate (if required)
			var federatedRequests = this.federator.SayHello(request);

			// wait for responses
			var helloFrom = string.Empty;
			var responseTasks = new List<Task<HelloReply>>();
			if (federatedRequests != null)
			{
				var headerTasks = new List<Task<Metadata>>(federatedRequests.Select(r => r.ResponseHeadersAsync));
				 responseTasks = new List<Task<HelloReply>>(federatedRequests.Select(r => r.ResponseAsync));
				Task.WaitAll(headerTasks.ToArray());
				helloFrom = string
					.Join(",", headerTasks
					.Select(t => t.Result
					.Where(header => string.Equals(header.Key, "X-GREET", StringComparison.OrdinalIgnoreCase))
					.Select(header => $"{header.Value}")));
			}

			// respond to caller
            var headerMetadata = new Metadata();
            headerMetadata.Add("X-GREET", $"[{instanceName}]{helloFrom}");
			context.WriteResponseHeadersAsync(headerMetadata);
			
			Task.WaitAll(responseTasks.ToArray());
			return Task.FromResult(new HelloReply { Message = request.Name });
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

            var downStreamHosts = Environment.GetEnvironmentVariable("GREETINGS_DOWNSTREAM_HOSTS") ?? string.Empty;

			int helloDelay;
			if (!int.TryParse(Environment.GetEnvironmentVariable("GREETINGS_DELAYMS"), out helloDelay))
			{
				helloDelay = 0;
			}
 
			var federator = new GreetingFederator(downStreamHosts, instanceName);

			Server server = new Server
			{
				Services = { Greeter.BindService(new GreeterImpl(instanceName, federator)) },
				Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
			};
			server.Start();

			Console.WriteLine($"Greeter server [{instanceName}] listening on port [{port}]");

			var cts = new CancellationTokenSource();
			if (helloDelay != 0)
			{
				int numBytes;
				string payload = string.Empty;
				if (int.TryParse(Environment.GetEnvironmentVariable("GREETINGS_NUMBYTES"), out numBytes))
				{
					if (numBytes > 0)
					{
						payload = new string('*', numBytes);
					}
				}

				Console.WriteLine($"Greeter server [{instanceName}] will federate as client");
				Task.Run(() => {
					while(!cts.IsCancellationRequested)
					{
						federator.SayHello(new HelloRequest {Name = payload});
						if (helloDelay > 0)
						{
							Task.Delay(helloDelay, cts.Token);
						}
					}
				}, cts.Token);
			}

			var syncTask = new TaskCompletionSource<bool>();
			
			System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += (context) => {
				Console.WriteLine("Greeter server received kill signal...");
				cts.Cancel();
				server.ShutdownAsync().Wait();
				syncTask.SetResult(true);
			};

			syncTask.Task.Wait(-1);
			Console.WriteLine("Greeter server stopped");
		}
	}
}
