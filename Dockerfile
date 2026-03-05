# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj", "Devken.CBC.SchoolManagement.API/"]
COPY ["Devken.CBC.SchoolManagement.Application/Devken.CBC.SchoolManagement.Application.csproj", "Devken.CBC.SchoolManagement.Application/"]
COPY ["Devken.CBC.SchoolManagement.Infrastructure/Devken.CBC.SchoolManagement.Infrastructure.csproj", "Devken.CBC.SchoolManagement.Infrastructure/"]
COPY ["Devken.CBC.SchoolManagement.Domain/Devken.CBC.SchoolManagement.Domain.csproj", "Devken.CBC.SchoolManagement.Domain/"]

RUN dotnet restore "Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj"

# Copy all source files
COPY Devken.CBC.SchoolManagement.API/. Devken.CBC.SchoolManagement.API/
COPY Devken.CBC.SchoolManagement.Application/. Devken.CBC.SchoolManagement.Application/
COPY Devken.CBC.SchoolManagement.Infrastructure/. Devken.CBC.SchoolManagement.Infrastructure/
COPY Devken.CBC.SchoolManagement.Domain/. Devken.CBC.SchoolManagement.Domain/

# Publish the API project
RUN dotnet publish "Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Install EF Core tools in the SDK stage so we can bundle the migration bundle
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Create a self-contained EF migration bundle (no SDK needed at runtime)
RUN dotnet ef migrations bundle \
    --project Devken.CBC.SchoolManagement.Infrastructure/Devken.CBC.SchoolManagement.Infrastructure.csproj \
    --startup-project Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj \
    --output /app/publish/efbundle \
    --self-contained \
    --force

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output (includes efbundle) from build stage
COPY --from=build /app/publish .

# Copy the startup script
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

# Expose port and set environment
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# CORS allowed origins (double-underscore = nested config in ASP.NET Core)
ENV Cors__AllowedOrigins__0=https://dev-ken-systems.vercel.app
ENV Cors__AllowedOrigins__1=http://localhost:4200

ENTRYPOINT ["./entrypoint.sh"]
