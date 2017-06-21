FROM microsoft/dotnet:1.0-runtime
WORKDIR /app
ENV GREETINGS_PORT 3000
EXPOSE 3000
COPY bin/Debug/netcoreapp1.0/publish /app
 
ENTRYPOINT ["dotnet", "GreeterServer.dll"]

