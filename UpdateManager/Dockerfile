﻿FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apt update
RUN apt install curl -y
RUN curl -fsSL https://get.docker.com -o get-docker.sh
RUN sh get-docker.sh

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UpdateManager/UpdateManager.csproj", "UpdateManager/"]
RUN dotnet restore "UpdateManager/UpdateManager.csproj"
COPY . .
WORKDIR "/src/UpdateManager"
RUN dotnet build "UpdateManager.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "UpdateManager.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UpdateManager.dll"]
