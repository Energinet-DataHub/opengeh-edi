FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Messaging.PerformanceTest/Messaging.PerformanceTest.csproj", "Messaging.PerformanceTest/"]
RUN dotnet restore "Messaging.PerformanceTest/Messaging.PerformanceTest.csproj"
COPY . .
WORKDIR "/src/Messaging.PerformanceTest"
RUN dotnet build "Messaging.PerformanceTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Messaging.PerformanceTest.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Messaging.PerformanceTest.dll"]
