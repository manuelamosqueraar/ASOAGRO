# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY ["ASOAGRO.csproj", "./"]
RUN dotnet restore "ASOAGRO.csproj"

# Copiar todo el código y compilar en Release
COPY . .
RUN dotnet publish "ASOAGRO.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto estándar
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ASOAGRO.dll"]

