FROM microsoft/dotnet:1.1-runtime
WORKDIR /app
COPY bin/Debug/netcoreapp1.0/publish /app
 
ENTRYPOINT ["dotnet", "GreeterClient.dll"]

