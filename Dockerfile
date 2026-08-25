FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["TimeCapsule.API.csproj", "./"]
RUN dotnet restore "./TimeCapsule.API.csproj"

COPY . .
RUN dotnet publish "TimeCapsule.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TimeCapsule.API.dll"]