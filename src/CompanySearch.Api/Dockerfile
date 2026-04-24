FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props CompanySearch.sln ./
COPY src/CompanySearch.Domain/CompanySearch.Domain.csproj src/CompanySearch.Domain/
COPY src/CompanySearch.Application/CompanySearch.Application.csproj src/CompanySearch.Application/
COPY src/CompanySearch.Infrastructure/CompanySearch.Infrastructure.csproj src/CompanySearch.Infrastructure/
COPY src/CompanySearch.Api/CompanySearch.Api.csproj src/CompanySearch.Api/

RUN dotnet restore CompanySearch.sln

COPY . .
RUN dotnet publish src/CompanySearch.Api/CompanySearch.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CompanySearch.Api.dll"]
