FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy the project file and restore dependencies
# Adjust the path if your folder structure is different
COPY ["InventarioService/InventarioService.csproj", "InventarioService/"]
COPY ["TiendaAspire.Data/TiendaAspire.Data.csproj", "TiendaAspire.Data/"]
COPY ["TiendaAspire.ServiceDefaults/TiendaAspire.ServiceDefaults.csproj", "TiendaAspire.ServiceDefaults/"]

# 2. Restore using the main project path
RUN dotnet restore "InventarioService/InventarioService.csproj"

#3 Copy the rest of the code and publish the app
COPY . .

# 4. Move to the project folder and Publish
WORKDIR "/src/InventarioService"
RUN ls -R
RUN dotnet publish "InventarioService.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app
COPY --from=build /app/publish .

# Environment variables for the container
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "InventarioService.dll"]
