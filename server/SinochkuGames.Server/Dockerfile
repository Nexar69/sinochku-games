FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY server/SinochkuGames.Server/SinochkuGames.Server.csproj server/SinochkuGames.Server/
RUN dotnet restore server/SinochkuGames.Server/SinochkuGames.Server.csproj
COPY server/SinochkuGames.Server/ server/SinochkuGames.Server/
RUN dotnet publish server/SinochkuGames.Server/SinochkuGames.Server.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /data
ENV ASPNETCORE_URLS=http://+:8080
ENV SINOCHKU_DATA_DIR=/data
EXPOSE 8080
ENTRYPOINT ["dotnet", "SinochkuGames.Server.dll"]
