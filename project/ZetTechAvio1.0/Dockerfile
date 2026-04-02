# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project file
COPY ["ZetTechAvio1.0.csproj", "./"]

# Restore dependencies
RUN dotnet restore "ZetTechAvio1.0.csproj"

# Copy entire project
COPY [".", "."]

# Build project
RUN dotnet build "ZetTechAvio1.0.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ZetTechAvio1.0.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 5151 5152

ENTRYPOINT ["dotnet", "ZetTechAvio1.0.dll"]
