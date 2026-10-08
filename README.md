# ASOAGRO - GUIA DE APOYO
# 🚨 REGLAS OBLIGATORIAS PARA TRABAJAR EN EL PROYECTO

Para no borrar el trabajo de los demás y mantener el proyecto al día, todos debemos seguir este flujo sin excepción.

---

## 🛑 REGLA 1: ANTES de empezar a programar o modificar algo

Cada vez que vayas a abrir VS Code para empezar a trabajar, **LO PRIMERO** que debes hacer es actualizar tu proyecto local con lo que subieron los demás.

1. Abre la terminal en VS Code (`Menú superior > Terminal > New Terminal`).
2. Escribe y ejecuta:

`git pull`

> **¿Por qué?** Si no haces `git pull` antes de tocar código, vas a trabajar sobre una versión vieja y vas a causar conflictos con los cambios que tus compañeros ya subieron.

---

## 🚀 REGLA 2: DESPUÉS de hacer cualquier cambio o crear algo

Cada vez que termines una pantalla, arregles un error o modifiques algún archivo, **LO OBLIGATORIO** es guardar tus archivos en VS Code con `Ctrl + S` y ejecutar estos 3 comandos en la terminal para subir tus avances:

### 1. Preparar todo lo que modificaste:
`git add .`

*(No olvides el punto `.` al final, sirve para incluir todos los archivos creados o editados).*

---

### 2. Guardar el avance explicando qué hiciste:
`git commit -m "Escribe aquí la descripción detallada de lo que hiciste"`

*Ejemplos:* 
* `git commit -m "Se creo el formulario de Editar Asociado y se agrego al menu"`
* `git commit -m "Se corrigieron los campos en Asociado.cs"`

---

### 3. Subir tus cambios a GitHub:
`git push`

> **¿Por qué?** Si haces cambios en tu PC pero no ejecutas `git add .`, `git commit` y `git push`, nadie más podrá ver tu trabajo y tus compañeros no tendrán tu código actualizado.

---

## 💡 Resumen rápido (Mano de santo):

1. 📥 **Antes de programar:** `git pull`
2. 💾 **Guardar en VS Code:** `Ctrl + S`
3. 📤 **Al terminar:** `git add .` ➔ `git commit -m "..."` ➔ `git push`

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

## Pruebas

Ejecutar toda la suite:

```bash
dotnet test Asoagro.slnx
```

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

Consultar alertas de documentos:

```http
GET /api/alertas-documentos
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

Respuestas relevantes:

- `200 OK`: operación exitosa.
- `400 Bad Request`: datos inválidos.
- `409 Conflict`: cédula duplicada.

Los errores del API usan una estructura común:

```json
{
  "code": "validation_error",
  "message": "La solicitud contiene datos inválidos.",
  "details": {
    "Cedula": ["La cédula es obligatoria."]
  }
}
```

## Funcionalidad actual

- Registro de asociados contra `POST /api/asociados`.
- Consulta de asociados contra `GET /api/asociados`.
- Validación básica con Data Annotations.
- Detección de cédulas duplicadas en memoria.
- Pantallas iniciales para documentos y alertas.

## Estructura del proyecto

- `Components/`: componentes y páginas Blazor.
- `Components/Pages/`: pantallas principales de la aplicación.
- `Components/Layout/`: layout general y navegación lateral.
- `Controllers/`: endpoints HTTP usados por la UI.
- `Models/`: modelos de dominio y validaciones básicas.
- `Services/`: servicios de aplicación. Actualmente contiene la gestión en memoria de asociados.
- `wwwroot/`: archivos estáticos como CSS, Bootstrap e íconos.
- `Configuration/`: opciones tipadas leídas desde `appsettings`.
- `Asoagro.Tests/`: pruebas automatizadas de API.

## Configuración

La configuración principal vive en:

- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Production.json`

Sección propia del sistema:

```json
"Asoagro": {
  "Audit": {
    "DefaultUser": "Sistema"
  }
}
```

`DefaultUser` se usa para auditoría básica mientras no exista autenticación real.

## Auditoría

El modelo `Asociado` guarda:

- `FechaRegistro`
- `FechaActualizacion`
- `CreadoPor`
- `ActualizadoPor`

Actualmente el usuario de auditoría viene desde configuración.

## Limitaciones actuales

- Los datos se guardan en memoria y se pierden al reiniciar la aplicación.
- El módulo de documentos todavía es simulado.
- Las alertas de vencimiento todavía usan un servicio en memoria con datos de ejemplo.
- No hay autenticación ni autorización.
- No hay persistencia real en base de datos.

## Próximos pasos recomendados

- Agregar persistencia con Entity Framework Core.
- Crear entidades para documentos y vencimientos.
- Implementar autenticación para el panel administrativo.
- Agregar pruebas básicas para API y componentes principales.

## Notas para colaboradores

- No subir carpetas generadas como `bin/` u `obj/`.
- No subir credenciales, rutas personales, archivos locales ni datos reales de usuarios.
- Usar datos de ejemplo en documentación, pruebas o capturas.
