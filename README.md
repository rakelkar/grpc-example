gRPC timing greeter client and server
=====================================

This is an example GRPC client and server based on the example in the GRPC repo.

The project depends on the [Grpc](https://www.nuget.org/packages/Grpc/)
and [Google.Protobuf](https://www.nuget.org/packages/Google.Protobuf/) NuGet packages.

PREREQUISITES FOR RUNNING IN CONTAINERS
---------------------------------------
None :) the repo uses Docker multi-state build


BUILD
-----
```
> cd greeter
> docker-compose build
```

RUN
---
```
> docker-compose up
```

Note: the client will fail since it is currently attempting to bind to localhost. 
Edit the compose file to put in your host name for GREETINGS_HOST


PREREQUISITES FOR RUNNING ON HOST
---------------------------------

- The DotNetCore SDK cli  available to download at https://www.microsoft.com/net/download


BUILD ON HOST
-------------

(you dont have to do this - see docker-compose instead)

From the `greeter/GreetingClient` (and Server) directories:

- `dotnet restore`

- `dotnet build **/project.json` (this will automatically download NuGet dependencies)

RUN ON HOST
-----------

(you dont have to do this - see docker-compose instead)

- Run the server

  ```
  > cd GreeterServer
  > dotnet run
  ```

- Run the client

  ```
  > cd GreeterClient
  > dotnet run
  ```


More
--------

You can find a more detailed tutorial about Grpc in [gRPC Basics: C#][]