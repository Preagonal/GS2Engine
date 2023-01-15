FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["GS2Engine.csproj", "./"]
RUN dotnet restore "GS2Engine.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "GS2Engine.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GS2Engine.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GS2Engine.dll"]
