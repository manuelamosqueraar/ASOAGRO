# ASOAGRO

Aplicación ASP.NET Core Blazor para la gestión inicial de asociados productores, documentos y alertas de vencimiento.

## Requisitos

- Git.
- .NET SDK 10 o una versión compatible con el `TargetFramework` definido en `Asoagro.csproj`.

Verificar que .NET esté instalado:

```bash
dotnet --info
```

Debe aparecer al menos un SDK instalado, por ejemplo:

```text
.NET SDKs installed:
  10.0.x
```

## Descargar el proyecto

```bash
git clone https://github.com/manuelamosqueraar/ASOAGRO.git
cd ASOAGRO
```

Si ya tienes el repositorio descargado, solo entra a la carpeta del proyecto:

```bash
cd ASOAGRO
```

## Ejecución local

Restaurar dependencias:

```bash
dotnet restore
```

Compilar el proyecto:

```bash
dotnet build
```

Ejecutar con el perfil HTTP:

```bash
dotnet run --launch-profile http
```

Abrir en el navegador:

```text
http://localhost:5162
```

## Ejecución con HTTPS

También puedes ejecutar el perfil HTTPS:

```bash
dotnet run --launch-profile https
```

URLs disponibles por defecto:

```text
https://localhost:7136
http://localhost:5162
```

Si el navegador muestra advertencia de certificado, confiar el certificado local de desarrollo:

```bash
dotnet dev-certs https --trust
```

Luego vuelve a ejecutar:

```bash
dotnet run
```

La aplicación expone páginas Blazor y endpoints API en el mismo proyecto.

## Cómo detener la aplicación

En la terminal donde se está ejecutando el proyecto, presiona:

```text
Ctrl + C
```

## Rutas principales

- `/`: página de inicio.
- `/registrar-asociado`: formulario para registrar asociados.
- `/consultar-asociados`: consulta de asociados registrados en memoria.
- `/registrar-documentos`: formulario inicial para documentos.
- `/alertas-documentos`: vista inicial de alertas.

## API disponible

Registrar asociado:

```http
POST /api/asociados
```

Consultar asociados:

```http
GET /api/asociados
```

Ejemplo de datos para registrar un asociado:

```json
{
  "cedula": "1234567890",
  "nombreCompleto": "Nombre de Ejemplo",
  "fincaVereda": "Finca de Ejemplo",
  "cultivoPrincipal": "Cacao",
  "tieneBPA": true
}
```

## Funcionalidad actual

- Registro de asociados contra `POST /api/asociados`.
- Consulta de asociados contra `GET /api/asociados`.
- Validación básica con Data Annotations.
- Detección de cédulas duplicadas en memoria.
- Pantallas iniciales para documentos y alertas.

## Limitaciones actuales

- Los datos se guardan en memoria y se pierden al reiniciar la aplicación.
- El módulo de documentos todavía es simulado.
- Las alertas de vencimiento todavía usan datos de ejemplo.
- No hay autenticación ni autorización.
- No hay pruebas automatizadas.

## Próximos pasos recomendados

- Agregar persistencia con Entity Framework Core.
- Crear entidades para documentos y vencimientos.
- Implementar autenticación para el panel administrativo.
- Agregar pruebas básicas para API y componentes principales.

## Notas para colaboradores

- No subir carpetas generadas como `bin/` u `obj/`.
- No subir credenciales, rutas personales, archivos locales ni datos reales de usuarios.
- Usar datos de ejemplo en documentación, pruebas o capturas.
