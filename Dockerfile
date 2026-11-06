# Etapa 1: Compilacion
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY ["Asoagro.csproj", "./"]
RUN dotnet restore "Asoagro.csproj"

# Copiar todo el codigo y compilar en Release
COPY . .
RUN dotnet publish "Asoagro.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen final de ejecucion
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto estandar
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Asoagro.dll"]
