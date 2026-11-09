FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Ditado.API/Ditado.API.csproj Ditado.API/
COPY Ditado.Aplicacao/Ditado.Aplicacao.csproj Ditado.Aplicacao/
COPY Ditado.Dominio/Ditado.Dominio.csproj Ditado.Dominio/
COPY Ditado.Infra/Ditado.Infra.csproj Ditado.Infra/
RUN dotnet restore Ditado.API/Ditado.API.csproj
COPY Ditado.API/ Ditado.API/
COPY Ditado.Aplicacao/ Ditado.Aplicacao/
COPY Ditado.Dominio/ Ditado.Dominio/
COPY Ditado.Infra/ Ditado.Infra/
RUN dotnet publish Ditado.API/Ditado.API.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ditado.API.dll"]
