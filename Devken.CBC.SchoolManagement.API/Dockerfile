# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files separately to leverage Docker cache
COPY ["Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj", "Devken.CBC.SchoolManagement.API/"]
COPY ["Devken.CBC.SchoolManagement.Application.Service/Devken.CBC.SchoolManagement.Application.Service.csproj", "Devken.CBC.SchoolManagement.Application.Service/"]
COPY ["Devken.CBC.SchoolManagement.Infrastructure/Devken.CBC.SchoolManagement.Infrastructure.csproj", "Devken.CBC.SchoolManagement.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj"

# Copy the rest of the source code
COPY . .

# Publish the application
RUN dotnet publish "Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose port 10000
EXPOSE 10000

# Use environment variables for connection string and JWT
# Example on Render: ConnectionStrings__DefaultConnection, JwtSettings__SecretKey
ENV ASPNETCORE_URLS=http://+:10000

# Set entrypoint
ENTRYPOINT ["dotnet", "Devken.CBC.SchoolManagement.API.dll"]