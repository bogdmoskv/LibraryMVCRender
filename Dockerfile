FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Solution1/LibraryMVC.Domain/LibraryMVC.Domain.csproj src/Solution1/LibraryMVC.Domain/
COPY src/Solution1/LibraryMVC.Infrastructure/LibraryMVC.Infrastructure.csproj src/Solution1/LibraryMVC.Infrastructure/
COPY src/Solution1/LibraryMVC.WebMVC/LibraryMVC.WebMVC.csproj src/Solution1/LibraryMVC.WebMVC/
RUN dotnet restore src/Solution1/LibraryMVC.WebMVC/LibraryMVC.WebMVC.csproj

COPY src/Solution1/ src/Solution1/
RUN dotnet publish src/Solution1/LibraryMVC.WebMVC/LibraryMVC.WebMVC.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LibraryMVC.WebMVC.dll"]
