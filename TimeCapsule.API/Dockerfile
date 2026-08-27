FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["TimeCapsule.API/TimeCapsule.API.csproj", "TimeCapsule.API/"]
RUN dotnet restore "TimeCapsule.API/TimeCapsule.API.csproj"

COPY . .
WORKDIR /src/TimeCapsule.API
RUN dotnet publish "TimeCapsule.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TimeCapsule.API.dll"]