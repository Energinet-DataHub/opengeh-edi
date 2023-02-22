FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TestSuites/WholeSaleApiStub/WholeSaleApiStub.csproj", "TestSuites/WholeSaleApiStub/"]
RUN dotnet restore "TestSuites/WholeSaleApiStub/WholeSaleApiStub.csproj"
COPY . .
WORKDIR "/src/TestSuites/WholeSaleApiStub"
RUN dotnet build "WholeSaleApiStub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WholeSaleApiStub.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WholeSaleApiStub.dll"]
