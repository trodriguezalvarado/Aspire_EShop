FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

#1 Copy project and restore
COPY ["TiendaAspire.Web/TiendaAspire.Web.csproj", "TiendaAspire.Web/"]
COPY ["TiendaAspire.Data/TiendaAspire.Data.csproj", "TiendaAspire.Data/"]
COPY ["TiendaAspire.ServiceDefaults/TiendaAspire.ServiceDefaults.csproj", "TiendaAspire.ServiceDefaults/"]

# 2. Restore using the main project path
RUN dotnet restore "TiendaAspire.Web/TiendaAspire.Web.csproj"

#3 Copy everything and publish
COPY . .


# 4. Move to the project folder and Publish
WORKDIR "/src/TiendaAspire.Web"

RUN dotnet publish "TiendaAspire.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

WORKDIR /app

# Required for Alpine to handle dates/currencies
RUN apk add --no-cache icu-libs

# Set the environment variable
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy ONLY the compiled files from the previous stage
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "TiendaAspire.Web.dll"]
