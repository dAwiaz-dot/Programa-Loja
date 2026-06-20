FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LojaSistema.Api/LojaSistema.Api.csproj LojaSistema.Api/
RUN dotnet restore LojaSistema.Api/LojaSistema.Api.csproj

COPY LojaSistema.Api/ LojaSistema.Api/
RUN dotnet publish LojaSistema.Api/LojaSistema.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LojaSistema.Api.dll"]
