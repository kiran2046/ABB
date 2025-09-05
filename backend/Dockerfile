FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Intellinspect.Backend.csproj", "."]
RUN dotnet restore "Intellinspect.Backend.csproj"
COPY . .
RUN dotnet build "Intellinspect.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Intellinspect.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directories for uploads and data
RUN mkdir -p /app/uploads /app/data

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Intellinspect.Backend.dll"]
