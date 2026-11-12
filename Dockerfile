FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar el proyecto y restaurar dependencias
COPY ["ChefMetrics-BackEnd.csproj", "./"]
COPY ["ChefMetrics.Contracts/ChefMetrics.Contracts.csproj", "ChefMetrics.Contracts/"]
RUN dotnet restore "ChefMetrics-BackEnd.csproj"

# Copiar el resto del código
COPY . .

# Compilar y publicar la API
RUN dotnet publish "ChefMetrics-BackEnd.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads && chown -R app:app /app/wwwroot/uploads
USER app
ENTRYPOINT ["dotnet", "ChefMetrics-BackEnd.dll"]
