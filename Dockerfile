FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj", "Devken.CBC.SchoolManagement.API/"]
COPY ["Devken.CBC.SchoolManagement.Application/Devken.CBC.SchoolManagement.Application.csproj", "Devken.CBC.SchoolManagement.Application/"]
COPY ["Devken.CBC.SchoolManagement.Infrastructure/Devken.CBC.SchoolManagement.Infrastructure.csproj", "Devken.CBC.SchoolManagement.Infrastructure/"]
COPY ["Devken.CBC.SchoolManagement.Domain/Devken.CBC.SchoolManagement.Domain.csproj", "Devken.CBC.SchoolManagement.Domain/"]

RUN dotnet restore "Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj"

COPY Devken.CBC.SchoolManagement.API/. Devken.CBC.SchoolManagement.API/
COPY Devken.CBC.SchoolManagement.Application/. Devken.CBC.SchoolManagement.Application/
COPY Devken.CBC.SchoolManagement.Infrastructure/. Devken.CBC.SchoolManagement.Infrastructure/
COPY Devken.CBC.SchoolManagement.Domain/. Devken.CBC.SchoolManagement.Domain/

RUN dotnet publish "Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet ef migrations bundle \
    --project Devken.CBC.SchoolManagement.Infrastructure/Devken.CBC.SchoolManagement.Infrastructure.csproj \
    --startup-project Devken.CBC.SchoolManagement.API/Devken.CBC.SchoolManagement.API.csproj \
    --output /app/publish/efbundle \
    --self-contained \
    --force

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["./entrypoint.sh"]