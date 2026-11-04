# Sistema de recetas - API

API REST para administrar recetas, categorías y usuarios. Está desarrollada con ASP.NET Core y .NET 10, usa Entity Framework Core para acceder a SQL Server, Identity para administrar usuarios y roles, y JSON Web Tokens (JWT) para autenticar las solicitudes.

## Contenido

- [Tecnologías y paquetes NuGet](#tecnologías-y-paquetes-nuget)
- [Estructura y conceptos](#estructura-y-conceptos)
- [Requisitos y ejecución](#requisitos-y-ejecución)
- [Configuración](#configuración)
- [Autenticación y autorización](#autenticación-y-autorización)
- [Modelos y DTO](#modelos-y-dto)
- [Endpoints](#endpoints)
- [OpenAPI y pruebas](#openapi-y-pruebas)
- [Base de datos y migraciones](#base-de-datos-y-migraciones)
- [Middleware y pipeline HTTP](#middleware-y-pipeline-http)
- [Notas sobre configuración y seguridad](#notas-sobre-configuración-y-seguridad)

## Tecnologías y paquetes NuGet

El proyecto tiene como destino `net10.0`. Estas son sus referencias directas declaradas en `20263RecetasApi.csproj`:

| Paquete | Versión | Uso en este proyecto |
|---|---:|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.14` | Añade el esquema Bearer que lee el JWT del encabezado HTTP y valida su firma, emisor, audiencia y vigencia. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `10.0.12` | Conecta ASP.NET Core Identity con EF Core; permite persistir usuarios, contraseñas protegidas, roles y sus relaciones en la base de datos. |
| `Microsoft.AspNetCore.OpenApi` | `10.0.12` | Genera el documento OpenAPI de los endpoints de ASP.NET Core. |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.12` | Proveedor de EF Core para SQL Server; permite traducir consultas LINQ a SQL y guardar cambios en esa base de datos. |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.12` | Herramientas de EF Core para crear, inspeccionar y aplicar migraciones desde la CLI o Visual Studio. Se marca como activo privado para que no se propague como dependencia de una aplicación consumidora. |

El proyecto se apoya además en las bibliotecas incluidas en ASP.NET Core y .NET para MVC/controladores, inyección de dependencias, configuración, validación de datos y serialización JSON. La serialización web usa `System.Text.Json` de la plataforma.

### Aclaraciones sobre paquetes mencionados anteriormente

- **No se usa AutoMapper.** Los DTO de respuesta se construyen explícitamente en el código (por ejemplo, con `Desde`) y los valores de entrada se asignan a los modelos en cada controlador.
- **No se usa Npgsql ni PostgreSQL.** La configuración actual registra `UseSqlServer`, así que el proveedor esperado es Microsoft SQL Server.
- **No se usa Scalar.AspNetCore.** Se expone un documento OpenAPI en desarrollo, pero no se ha agregado una interfaz web de Scalar para navegarlo o probarlo.
- **No se usa Newtonsoft.Json.** La API usa el serializador JSON que ASP.NET Core proporciona por defecto, `System.Text.Json`.
- OpenAPI describe la API; no reemplaza la autenticación ni la autorización de los endpoints.

## Estructura y conceptos

Las carpetas principales del código son:

| Ruta | Responsabilidad |
|---|---|
| `Controllers/` | Controladores HTTP para autenticación, recetas, categorías y administración de usuarios. |
| `DTO/` | Tipos de entrada y salida de la API; separan el contrato HTTP de las entidades persistidas. |
| `Models/` | Entidades del dominio, como `Receta`, `Categoria` y el usuario de Identity. |
| `Data/ApplicationDbContext.cs` | Contexto de EF Core. Hereda de `IdentityDbContext<Usuario>` y expone las tablas de recetas y categorías. |
| `Auth/` | Servicios de autenticación y emisión de JWT, configuración JWT, roles y creación inicial de roles/administrador. |
| `Migrations/` | Historial y snapshot de cambios del esquema de la base de datos. |
| `Program.cs` | Registro de servicios y configuración del pipeline HTTP. |

**ORM** significa *Object-Relational Mapper* (mapeador objeto-relacional). EF Core desempeña ese papel: relaciona entidades C# con tablas, permite consultar con LINQ y guarda cambios sin escribir a mano el SQL de cada operación. El proveedor de SQL Server es el que adapta esas operaciones a SQL Server.

Un **modelo** representa los datos de dominio y su persistencia. Un **DTO** (*Data Transfer Object*) define qué datos acepta o devuelve una operación HTTP. Separarlos evita acoplar el formato público de la API directamente a todas las columnas internas de una entidad; también permite validar las entradas y devolver solo los campos previstos.

## Requisitos y ejecución

1. Instala el SDK de .NET 10 y una instancia accesible de Microsoft SQL Server.
2. Desde la raíz del repositorio, configura la cadena de conexión y los secretos de desarrollo como se explica en [Configuración](#configuración).
3. Restaura y compila el proyecto:

   ```powershell
   dotnet restore
   dotnet build
   ```

4. Inicia la API:

   ```powershell
   dotnet run
   ```

Los perfiles incluidos en `Properties/launchSettings.json` inician el entorno como `Development`. Las direcciones locales configuradas son `http://localhost:5262` y `https://localhost:7054`.

En desarrollo, la aplicación aplica automáticamente las migraciones pendientes al arrancar. En todos los entornos intenta crear los roles conocidos; si todavía no hay un administrador, también intenta crear el administrador inicial usando los valores `SeedAdmin` de la configuración. Si faltan esas credenciales, registra una advertencia y no crea esa cuenta.

## Configuración

### Cadena de conexión

`Program.cs` lee la conexión `DefaultConnection`:

```text
ConnectionStrings:DefaultConnection
```

La conexión que se use debe apuntar a una base de datos de SQL Server y tener permisos suficientes para las operaciones que correspondan. Para desarrollo, se puede guardar una conexión propia en User Secrets, en lugar de dejar credenciales o información de infraestructura en un archivo versionado:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=SERVIDOR;Database=RecetasDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

Adapta los parámetros a la instancia y al método de autenticación de SQL Server de tu entorno. No guardes contraseñas reales en el repositorio.

### JWT y administrador inicial

La sección `Jwt` debe proporcionar `Issuer`, `Audience`, `ExpirationMinutes` y `Key`. La clave debe tener al menos 32 caracteres; la aplicación comprueba la configuración al iniciar y no arranca si falta la sección o la clave no alcanza esa longitud. El valor predeterminado de expiración en `JwtSettings` es 30 minutos, y el valor efectivo es el que indique la configuración.

Guarda la clave con User Secrets para desarrollo:

```powershell
dotnet user-secrets set "Jwt:Key" "REEMPLAZAR_POR_UN_SECRETO_ALEATORIO_DE_32_CARACTERES_O_MAS"
```

El administrador inicial se configura con `SeedAdmin:Correo` y `SeedAdmin:Password`. `SeedAdmin:Nombre` y `SeedAdmin:Apellidos` son opcionales (si se omiten, el seeder usa `Administrador` y `Sistema`):

```powershell
dotnet user-secrets set "SeedAdmin:Correo" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "REEMPLAZAR_POR_UNA_CLAVE_SEGURA"
dotnet user-secrets set "SeedAdmin:Nombre" "Administrador"
dotnet user-secrets set "SeedAdmin:Apellidos" "Sistema"
```

`UserSecretsId` ya está definido en el archivo del proyecto. User Secrets está pensado para desarrollo local; en producción, suministra esos valores con un gestor de secretos o variables de entorno protegidas. La clave JWT y la contraseña de administración no deben escribirse en `appsettings.json`, en el README ni en el control de versiones.

Los archivos `appsettings.json` y `appsettings.Development.json` pueden contener valores no secretos y valores de desarrollo. La configuración de .NET combina sus proveedores; una fuente externa como User Secrets en desarrollo puede sustituir las claves correspondientes.

## Autenticación y autorización

La API implementa autenticación propia mediante **ASP.NET Core Identity y JWT**; no es inicio de sesión de Microsoft. Identity administra los usuarios y roles, almacena las contraseñas mediante sus mecanismos de hash y usa el contexto de EF Core como almacén. El cliente se autentica enviando correo y contraseña a `POST /api/auth/login`. Si las credenciales son válidas y el usuario está activo, recibe un token firmado con HMAC-SHA256, su fecha de expiración y un resumen del usuario.

Para llamar a un endpoint protegido, incluye el token sin comillas ni prefijo adicional en el encabezado:

```http
Authorization: Bearer <token>
```

En `Program.cs` se configura una política de autorización de respaldo que exige un usuario autenticado en **todos** los endpoints. El único endpoint público es el inicio de sesión, marcado expresamente con `[AllowAnonymous]`. Por tanto, las rutas de recetas y categorías también requieren JWT, aunque no declaren `[Authorize]` localmente. Los endpoints de `api/usuarios` tienen además `[Authorize(Roles = "Admin")]`, por lo que exigen el rol `Admin`.

Los roles que crea el seeder son:

- `Admin`: además de acceder a los endpoints autenticados generales, puede administrar cuentas con `api/usuarios`.
- `Recepcion`: puede acceder a endpoints que solo requieren autenticación. No tiene permiso para administrar usuarios.

`Roles.Personal` agrupa los nombres `Admin` y `Recepcion` para usarlo en una autorización por rol si se aplica a endpoints. Actualmente el controlador de usuarios exige específicamente `Admin`.

### Validación del token y cuentas

Al recibir un JWT, el middleware valida la firma, el emisor (`Issuer`), la audiencia (`Audience`) y la expiración (`Lifetime`); admite una tolerancia de reloj de 30 segundos. El token contiene el identificador, nombre, correo cuando existe, identificador único del token y roles del usuario. La identidad y el rol se representan como claims de ASP.NET Core.

Además, en cada solicitud autenticada se consulta Identity para confirmar que el usuario siga existiendo y esté activo. Si la cuenta fue desactivada/eliminada lógicamente, el token deja de servir aunque todavía no haya expirado. También se rechaza un token si contiene algún rol que el usuario ya no tiene; iniciar sesión otra vez obtiene un token con los roles actuales.

Configuración de contraseñas aplicada por Identity:

- Longitud mínima: 8 caracteres.
- Debe incluir al menos un número, una mayúscula, una minúscula y un carácter no alfanumérico.
- Correo único.
- Bloqueo temporal tras 5 intentos fallidos; el bloqueo dura 5 minutos.
- Un inicio de sesión correcto restablece el contador de intentos fallidos.

El inicio de sesión responde `401 Unauthorized` con un mensaje genérico ante correo desconocido o contraseña incorrecta, para no revelar si un correo está registrado; responde `423 Locked` si la cuenta está bloqueada y `403 Forbidden` si la contraseña es correcta pero la cuenta está desactivada.

## Modelos y DTO

### Entidades

- **`Receta`**: `Id`, `Nombre`, `Descripcion` opcional, `Ingredientes`, `Preparacion` y `CategoriaId`, con una referencia de navegación opcional a `Categoria`.
- **`Categoria`**: `id`, `Nombre` y `Descripcion` opcional. En la entidad la propiedad identificadora está escrita `id`; el DTO público la presenta como `Id`.
- **`Usuario`**: extiende `IdentityUser` y añade `Nombre`, `Apellido`, `Activo` y `FechaCreacion`. Identity aporta, entre otras propiedades, el identificador y el correo electrónico. La respuesta pública no expone el hash de contraseña ni las propiedades internas completas de Identity.

### DTO de recetas

- `RecetaCrearDto`: requiere `Nombre`, `Ingredientes` y `Preparacion`; admite `Descripcion` opcional y requiere `CategoriaId` mayor que cero. El endpoint comprueba que la categoría exista.
- `RecetaActualizarDto`: mismos campos y validaciones que el DTO de creación; `PUT` reemplaza esos campos de la receta.
- `RecetaResponseDto`: devuelve `Id`, `Nombre`, `Descripcion`, `Ingredientes`, `Preparacion`, `CategoriaId` y la categoría asociada como `CategoriaResponseDto` cuando está disponible.

### DTO de categorías

- `CategoriaCrearDto` y `CategoriaActualizarDto`: requieren `Nombre` y permiten `Descripcion` nula. `PUT` actualiza ambos campos, por lo que una descripción omitida/null queda nula.
- `CategoriaResponseDto`: devuelve `Id`, `Nombre` y `Descripcion`.

### DTO de autenticación y usuarios

- `LoginDto`: correo requerido con formato de email y máximo de 256 caracteres; contraseña requerida y máximo de 100.
- `LoginRespuestaDto`: JWT (`Token`), expiración UTC (`ExpiraUtc`) y datos públicos del usuario (`Usuario`).
- `CambiarPasswordDto`: `PasswordActual` y `PasswordNueva`, requeridas y con máximo de 100 caracteres. Identity valida la contraseña nueva y la contraseña actual.
- `UsuarioCrearDto`: nombre (máximo 80), apellidos (máximo 100), correo válido (máximo 256), contraseña (8-100 caracteres) y rol requerido.
- `UsuarioActualizarDto`: nombre, apellidos y correo con los mismos límites de longitud; actualiza datos de perfil, no cambia la contraseña ni el rol.
- `UsuarioCambiarRolDto`: rol requerido. La operación reemplaza los roles anteriores por el indicado.
- `UsuarioCambiarEstadoDto`: valor `Activo` requerido para activar o desactivar la cuenta.
- `RestablecerPasswordDto`: nueva contraseña requerida de 8 a 100 caracteres; la operación administrativa no solicita la contraseña anterior.
- `UsuarioRespuestaDto`: `Id`, `Nombre`, `Apellidos`, `Correo`, `Activo`, `FechaCreacion` y lista `Roles`.

Los controladores llevan `[ApiController]`, por lo que los errores de validación de anotaciones de datos se procesan como respuestas HTTP de validación. La API construye las respuestas a partir de las entidades manualmente; no hay perfiles ni configuración de AutoMapper.

## Endpoints

Todas las rutas usan el prefijo `/api`. Salvo `POST /api/auth/login`, requieren `Authorization: Bearer <token>`. Las rutas de usuarios requieren además que el token tenga el rol `Admin`.

### Autenticación

| Método y ruta | Acceso | Entrada / resultado |
|---|---|---|
| `POST /api/auth/login` | Público | Recibe `LoginDto`; devuelve `LoginRespuestaDto`. Errores: `401` credenciales no válidas, `403` cuenta inactiva, `423` cuenta bloqueada. |
| `GET /api/auth/me` | Cualquier usuario autenticado | Devuelve el perfil (`UsuarioRespuestaDto`) y roles asociados al usuario del token. |
| `POST /api/auth/cambiar-password` | Cualquier usuario autenticado | Recibe `CambiarPasswordDto` y cambia la contraseña del propio usuario, no la de otro. Devuelve `204` si se completó o `400` con errores de Identity si falló. |

Ejemplo de inicio de sesión:

```json
{
  "correo": "admin@example.com",
  "password": "LaClaveConfigurada"
}
```

Ejemplo de uso de la respuesta en una solicitud posterior:

```http
GET /api/auth/me
Authorization: Bearer <Token_recibido_del_login>
```

### Categorías

Todas requieren un usuario autenticado; no están limitadas al rol `Admin`.

| Método y ruta | Operación | Resultado relevante |
|---|---|---|
| `GET /api/categorias` | Lista categorías ordenadas por nombre. | `200` con `CategoriaResponseDto[]`. |
| `GET /api/categorias/{id}` | Obtiene una categoría. | `200` o `404` si no existe. |
| `POST /api/categorias` | Crea una categoría con `CategoriaCrearDto`. | `201` con el recurso y su URL de consulta. |
| `PUT /api/categorias/{id}` | Reemplaza nombre y descripción con `CategoriaActualizarDto`. | `200` o `404`. |
| `DELETE /api/categorias/{id}` | Elimina una categoría solo si no tiene recetas asociadas. | `204`, `404` o `409 Conflict` si tiene recetas. |

Ejemplo de cuerpo para crear o actualizar:

```json
{
  "nombre": "Postres",
  "descripcion": "Recetas dulces"
}
```

### Recetas

Todas requieren un usuario autenticado; no están limitadas al rol `Admin`.

| Método y ruta | Operación | Resultado relevante |
|---|---|---|
| `GET /api/recetas` | Lista recetas ordenadas por nombre. Admite el filtro opcional `?categoriaId={id}`. | `200` con `RecetaResponseDto[]`. |
| `GET /api/recetas/{id}` | Obtiene una receta y su categoría. | `200` o `404` si no existe. |
| `POST /api/recetas` | Crea una receta con `RecetaCrearDto`; la categoría indicada debe existir. | `201` con el recurso y su URL; `400` si la categoría no existe. |
| `PUT /api/recetas/{id}` | Reemplaza los campos de la receta con `RecetaActualizarDto`; la categoría indicada debe existir. | `200`, `404` si no existe la receta o `400` si no existe la categoría. |
| `DELETE /api/recetas/{id}` | Elimina una receta. | `204` o `404` si no existe. |

Ejemplo de cuerpo para crear o actualizar:

```json
{
  "nombre": "Panqueques",
  "descripcion": "Panqueques sencillos para el desayuno",
  "ingredientes": "1 taza de harina; 1 huevo; 1 taza de leche",
  "preparacion": "Mezclar los ingredientes y cocinar porciones en una sartén caliente.",
  "categoriaId": 1
}
```

### Administración de usuarios

Todas las rutas requieren el rol `Admin`. Los identificadores son los IDs de usuario de Identity, no IDs numéricos.

| Método y ruta | Operación | Resultado relevante |
|---|---|---|
| `GET /api/usuarios` | Lista usuarios, incluidos los desactivados, ordenados por apellidos y nombre. | `200` con `UsuarioRespuestaDto[]`. |
| `GET /api/usuarios/{id}` | Consulta un usuario por ID. | `200` o `404`. |
| `POST /api/usuarios` | Crea usuario activo y le asigna el rol indicado. | `201`; `400` por rol inválido u otros errores de Identity; `409` si correo/nombre de usuario ya existe. |
| `PUT /api/usuarios/{id}` | Actualiza nombre, apellidos y correo. Cambiar correo lo marca como no confirmado. | `200`, `400`, `404` o `409` si el correo ya existe. |
| `PUT /api/usuarios/{id}/rol` | Reemplaza los roles por el rol indicado. | `200`, `400` si el rol no existe, `404` o `409` si se intenta quitar el rol al último administrador activo. |
| `PUT /api/usuarios/{id}/restablecer-password` | Restablece la contraseña sin requerir la anterior. | `204`, `400` por problemas de validación de Identity o `404`. |
| `PATCH /api/usuarios/{id}/estado` | Recibe `{ "activo": true }` o `{ "activo": false }` para cambiar el estado. | `200`, `400`, `404` o `409` si se desactiva a sí mismo o al último administrador activo. |
| `DELETE /api/usuarios/{id}` | Desactivación lógica: establece `Activo = false`, no borra el registro. | `204`, `404` o `409` si se intenta desactivarse a sí mismo o desactivar al último administrador activo. |

La aplicación crea los roles `Admin` y `Recepcion`. No se pueden asignar nombres de rol que no existan. Al eliminar o desactivar una cuenta se conserva su información en la base de datos, pero el usuario ya no puede autenticarse.

## OpenAPI y pruebas

En el entorno `Development`, la API publica el documento OpenAPI en:

```text
http://localhost:5262/openapi/v1.json
https://localhost:7054/openapi/v1.json
```

El documento está habilitado anónimamente para que pueda consultarse aunque la política general exija autenticación. No se configura una interfaz interactiva de Scalar ni Swagger UI en el código actual. Para probar solicitudes, usa un cliente HTTP que permita enviar el encabezado Bearer o el archivo [`20263RecetasApi.http`](./20263RecetasApi.http) desde Visual Studio Code/Visual Studio; reemplaza sus variables de ejemplo por el token y los identificadores de tu entorno.

## Base de datos y migraciones

`ApplicationDbContext` hereda de `IdentityDbContext<Usuario>`. El esquema reúne las entidades de la aplicación (categorías y recetas) y las tablas de Identity (usuarios, roles, claims, inicios de sesión, tokens y relaciones usuario-rol). Las migraciones registradas en `Migrations/` mantienen el esquema de SQL Server alineado con los modelos.

En desarrollo, `Program.cs` ejecuta `Database.MigrateAsync()` al iniciar. Esto aplica las migraciones pendientes automáticamente **solo si** el entorno es `Development`. En otros entornos no se aplica el esquema automáticamente; planifica y ejecuta la migración de forma controlada durante el despliegue.

Para crear una migración después de cambiar un modelo:

```powershell
dotnet ef migrations add NombreDescriptivo
```

Para aplicar las migraciones manualmente:

```powershell
dotnet ef database update
```

Para retirar la última migración que todavía no se ha aplicado a la base de datos:

```powershell
dotnet ef migrations remove
```

Usa nombres descriptivos y únicos para las migraciones. Si una migración ya se aplicó o fue compartida con otros entornos, no la borres ni la vuelvas a crear: agrega una nueva migración que represente el siguiente cambio. El comando `dotnet ef` requiere que esté disponible la herramienta de EF Core CLI (`dotnet-ef`) en el entorno de desarrollo; el paquete `Microsoft.EntityFrameworkCore.Tools` ya está declarado por el proyecto.

## Middleware y pipeline HTTP

El pipeline se configura en `Program.cs`. Un **middleware** es un componente que procesa una solicitud y su respuesta dentro de una cadena ordenada; la posición importa porque cada componente puede inspeccionar, modificar o detener el procesamiento.

En esta API:

1. En desarrollo se registran el documento OpenAPI y la aplicación de migraciones pendientes.
2. `UseHttpsRedirection()` redirige solicitudes HTTP a HTTPS cuando existe un endpoint HTTPS configurado.
3. `UseAuthentication()` valida las credenciales Bearer y establece el usuario/claims de la solicitud.
4. `UseAuthorization()` evalúa la política de respaldo y los requisitos de roles.
5. `MapControllers()` registra las rutas de los controladores.

La autenticación debe ir antes de la autorización para que las políticas y `[Authorize]` puedan consultar la identidad ya establecida.

`AllowedHosts` es una opción de filtrado del encabezado `Host` de ASP.NET Core; **no** es una lista de IPs cliente autorizadas ni reemplaza CORS, autenticación, firewall o reglas de red. El valor `"*"` permite cualquier valor de `Host`, por lo que en despliegues conviene restringirlo a los nombres de host esperados según la infraestructura.

## Notas sobre configuración y seguridad

- Usa HTTPS fuera de entornos locales y no transmitas el token en la URL; envíalo en el encabezado `Authorization`.
- Protege la cadena de conexión, la clave JWT y las credenciales del administrador inicial con User Secrets en desarrollo y un almacén seguro en producción.
- Si cambian las claves JWT, los tokens firmados con la clave anterior dejan de validarse. Si cambia un rol, se recomienda volver a iniciar sesión para recibir los claims actuales.
- El documento OpenAPI se habilita únicamente en `Development`; no se debe asumir que está publicado en producción.
- Las operaciones de actualización con `PUT` reemplazan los campos contemplados por el DTO, no son actualizaciones parciales. La modificación parcial de estado de usuario es la excepción y se realiza con `PATCH`.
