FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY server/ .
RUN dotnet restore MmoDemo.sln
RUN dotnet publish src/MmoDemo.Gateway/MmoDemo.Gateway.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
COPY configs/ /app/configs/
COPY resources/ /app/resources/
EXPOSE 5000
ENTRYPOINT ["dotnet", "MmoDemo.Gateway.dll"]
