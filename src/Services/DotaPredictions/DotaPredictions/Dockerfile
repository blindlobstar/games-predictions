#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["DotaPredictions/DotaPredictions.csproj", "DotaPredictions/"]
COPY ["../../Basics/EventBus.Core/EventBus.Core.csproj", "../../Basics/EventBus.Core/"]
COPY ["../../Basics/EventBus.RabbitMq/EventBus.RabbitMq.csproj", "../../Basics/EventBus.RabbitMq/"]
RUN dotnet restore "DotaPredictions/DotaPredictions.csproj"
COPY . .
WORKDIR "/src/DotaPredictions"
RUN dotnet build "DotaPredictions.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotaPredictions.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotaPredictions.dll"]