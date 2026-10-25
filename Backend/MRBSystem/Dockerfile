# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MRBSystem.sln", "./"]
COPY ["core/MRBSystem.Domain/MRBSystem.Domain.csproj", "core/MRBSystem.Domain/"]
COPY ["core/MRBSystem.Shared/MRBSystem.Shared.csproj", "core/MRBSystem.Shared/"]
COPY ["core/MRBSystem.Contracts/MRBSystem.Contracts.csproj", "core/MRBSystem.Contracts/"]
COPY ["core/MRBSystem.Application/MRBSystem.Application.csproj", "core/MRBSystem.Application/"]
COPY ["infrasturucture/MRBSystem.Persistence/MRBSystem.Persistence.csproj", "infrasturucture/MRBSystem.Persistence/"]
COPY ["infrasturucture/MRBSystem.Infrasturucture/MRBSystem.Infrasturucture.csproj", "infrasturucture/MRBSystem.Infrasturucture/"]
COPY ["presentation/MRBSystem.WebAPI/MRBSystem.WebAPI.csproj", "presentation/MRBSystem.WebAPI/"]
COPY ["MRBSystem.Testing/MRBSystem.Testing.csproj", "MRBSystem.Testing/"]

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build the WebAPI project
RUN dotnet build -c Release -o /app/build presentation/MRBSystem.WebAPI/MRBSystem.WebAPI.csproj

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish presentation/MRBSystem.WebAPI/MRBSystem.WebAPI.csproj

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published files from publish stage
COPY --from=publish /app/publish .

# Create logs directory for Serilog
RUN mkdir -p /app/Logs

# Expose port
EXPOSE 8080 8443

# Set environment for ASP.NET Core
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "MRBSystem.WebAPI.dll"]
