# Muyu Backend

Backend de Muyu construido como monolito modular en .NET 8, aplicando DDD y CQRS:

- `Muyu.Shared`: entidades base, contrato de configuración modular y `MuyuDbContext` global.
- `Muyu.Modules.IAM`: gestión de usuarios, registro de agricultores/comerciantes, perfiles, roles, autenticación y JWT.
- `Muyu.Modules.ParcelManagement`: registro, límites GPS y ciclo de vida de parcelas.
- `Muyu.Modules.Agreements`: acuerdos de financiamiento y planes ordenados de desembolso.
- `Muyu.Modules.Escrow`: cuentas de custodia, fondos, reservas y pagos.
- `Muyu.Modules.Tracking`: hitos, evidencias, almacenamiento y revisión.
- `Muyu.Modules.ClimateAlerts`: monitoreo meteorológico y alertas por umbrales.
- `Muyu.API`: composición, SQLite, CORS, JWT y Swagger/OpenAPI.

La estructura física separa el host ejecutable, los bounded contexts y los bloques compartidos:

```text
src/
├── Host/
│   └── Muyu.API/
├── BoundedContexts/
│   ├── Agreements/
│   ├── ClimateAlerts/
│   ├── Escrow/
│   ├── IAM/
│   ├── ParcelManagement/
│   └── Tracking/
└── BuildingBlocks/
    └── Muyu.Shared/
```

Cada bounded context contiene únicamente estas capas:

```text
Domain/
Application/
Infrastructure/
Presentation/
<Module>.csproj
```

Los artefactos temporales `bin` y `obj` no son capas ni código fuente. `Directory.Build.props`
los redirige a `work/build`, fuera de los módulos.

## Ejecutar

```powershell
dotnet restore Muyu.sln
dotnet run --project src/Host/Muyu.API/Muyu.API.csproj
```

Swagger queda disponible en `/swagger` cuando el entorno es `Development`.

## Desplegar en Railway

El proyecto incluye un `Dockerfile` para .NET 8, escucha el puerto dinámico `PORT` y expone
`GET /health`. Para conservar SQLite entre despliegues debe montarse un volumen en `/data`.
La guía completa y las variables necesarias están en [RAILWAY.md](RAILWAY.md).

Para desarrollo se crea un usuario inicial desde `appsettings.Development.json`:

- Correo: `admin@muyu.local`
- Contraseña: `Muyu.Dev.2026!`

Prueba de login:

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@muyu.local",
  "password": "Muyu.Dev.2026!"
}
```

> Antes de desplegar, reemplace `Jwt:SigningKey` por un secreto suministrado mediante
> variables de entorno o un gestor de secretos. El usuario semilla solo existe en Development.

Para activar el monitoreo climático configure `OpenWeather:ApiKey` mediante secretos de usuario
o variables de entorno. Para evidencias puede configurar `Storage:EvidencePath`; si se omite se
utiliza `evidence-storage` junto a los binarios de ejecución.

## Endpoints principales

- `POST /api/v1/auth/register/agricultor`
- `POST /api/v1/auth/register/comerciante`
- `POST /api/v1/auth/login`
- `GET /api/v1/users/me`
- `POST|GET /api/parcels`
- `POST|GET /api/agreements`
- `POST|GET /api/escrow`
- `POST|GET /api/milestones`
- `POST /api/milestones/{id}/evidences`
- `POST /api/climate-alerts/monitor`
- `GET /api/climate-alerts?parcelId={id}`

## Decisiones de arquitectura

### DDD por bounded context

Cada bounded context conserva su propio lenguaje, agregados, reglas y puertos. Las dependencias
apuntan hacia adentro: `Presentation` usa contratos de `Application`; `Infrastructure` implementa
los puertos de `Application`; `Domain` no depende de ASP.NET Core, EF Core ni proveedores externos.

Los módulos se relacionan mediante identificadores (`ParcelId`, `AgreementId`, etc.), no mediante
navegaciones EF ni referencias a entidades de otro bounded context. Cada agregado protege sus
invariantes y registra hechos de negocio como eventos de dominio inmutables.

### CQRS

`Application` no contiene servicios genéricos que mezclen lecturas y escrituras. Cada operación
posee un comando o una consulta y un handler dedicado:

- Los `ICommandHandler` cargan agregados con seguimiento, ejecutan comportamiento de dominio y
  persisten la transacción.
- Los `IQueryHandler` no modifican estado y leen con `AsNoTracking` para producir DTOs de respuesta.
- Los controladores solo traducen HTTP a comandos/consultas; no contienen reglas de negocio.

### Eventos de dominio

`AggregateRoot` mantiene los eventos generados en memoria. `MuyuDbContext` guarda primero la
transacción, publica después los eventos mediante `IDomainEventPublisher` y finalmente limpia el
buffer del agregado. El publicador actual es interno al proceso y permite añadir handlers entre
bounded contexts sin acoplar sus dominios. En un despliegue distribuido puede sustituirse por un
Outbox y un broker sin cambiar los agregados.

Cada módulo registra su configuración de entidades mediante `IModuleEntityConfiguration`.
Así `MuyuDbContext` sigue siendo global, pero `Muyu.Shared` no conoce las entidades de los
módulos ni introduce dependencias circulares. `EnsureCreated` facilita esta primera ejecución;
al evolucionar el esquema se debe sustituir por migraciones de EF Core.

El IAM incluye temporalmente `IamSchemaUpgrade` para actualizar sin pérdida la base SQLite creada
por la primera versión del prototipo. La validación DNI/RUC usa un adaptador local de demostración;
el puerto `IIdentityVerificationProvider` permite conectar RENIEC/SUNAT posteriormente.

La correspondencia entre el reporte, los diagramas C4 y la implementación está documentada en
[`docs/ARCHITECTURE-ALIGNMENT.md`](docs/ARCHITECTURE-ALIGNMENT.md).
