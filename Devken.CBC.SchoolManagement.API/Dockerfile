# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj", "Devken.CBC.SchoolManagement.API/"]
COPY ["Devken.CBC.SchoolManagement.Application/Devken.CBC.SchoolManagement.Application.csproj", "Devken.CBC.SchoolManagement.Application/"]
COPY ["Devken.CBC.SchoolManagement.Infrastructure/Devken.CBC.SchoolManagement.Infrastructure.csproj", "Devken.CBC.SchoolManagement.Infrastructure/"]
COPY ["Devken.CBC.SchoolManagement.Domain/Devken.CBC.SchoolManagement.Domain.csproj", "Devken.CBC.SchoolManagement.Domain/"]
RUN dotnet restore "Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj"
COPY . .
RUN dotnet publish "Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENTRYPOINT ["dotnet", "Devken.CBC.SchoolManagement.API.dll"]