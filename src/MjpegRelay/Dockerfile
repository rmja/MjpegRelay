#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/MjpegRelay/MjpegRelay.csproj", "src/MjpegRelay/"]
RUN dotnet restore "src/MjpegRelay/MjpegRelay.csproj"
COPY . .
WORKDIR "/src/src/MjpegRelay"
RUN dotnet build "MjpegRelay.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MjpegRelay.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

#ENTRYPOINT ["dotnet", "MjpegRelay.dll"]

COPY start.sh /usr/src/
CMD [ "/bin/bash", "/usr/src/start.sh" ]

