# Despliegue de Muyu en Railway Free

Muyu conserva SQLite. La API, la base de datos, las evidencias y las claves de protección se
ejecutan dentro de un único servicio de Railway con un volumen persistente.

## 1. Crear el servicio

1. Suba este directorio a un repositorio de GitHub.
2. En Railway seleccione **New Project > Deploy from GitHub repo**.
3. Seleccione el repositorio. Railway detectará automáticamente el `Dockerfile` de la raíz.
4. Mantenga una sola réplica. SQLite no debe compartirse entre varias réplicas.

## 2. Crear el volumen persistente

En el canvas del proyecto, agregue un volumen al servicio de la API y móntelo en:

```text
/data
```

El plan Free permite un volumen. La base SQLite, las evidencias y las claves se guardan dentro
de subdirectorios de ese mismo volumen.

## 3. Configurar variables

Agregue estas variables en **Service > Variables**:

```text
ConnectionStrings__MuyuDatabase=Data Source=/data/muyu.db
Storage__EvidencePath=/data/evidences
Storage__DataProtectionPath=/data/keys
Jwt__Issuer=Muyu.API
Jwt__Audience=Muyu.Client
Jwt__SigningKey=REEMPLAZAR_POR_UN_SECRETO_ALEATORIO_DE_64_CARACTERES
Jwt__ExpirationMinutes=60
Iam__SeedUser__Email=admin@muyu.local
Iam__SeedUser__Password=REEMPLAZAR_POR_UNA_CLAVE_ADMIN_SEGURA
Iam__SeedUser__DisplayName=Administrador Muyu
OpenWeather__ApiKey=REEMPLAZAR_POR_LA_API_KEY
ASPNETCORE_ENVIRONMENT=Production
```

No configure `PORT`: Railway la inyecta automáticamente y la API ya está preparada para
escuchar en `0.0.0.0` usando ese valor.

Para generar el secreto JWT desde PowerShell:

```powershell
$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

Si existe un frontend desplegado, agregue también su origen exacto:

```text
Cors__AllowedOrigins__0=https://su-frontend.example
```

## 4. Networking y healthcheck

1. En **Settings > Networking**, genere un dominio público.
2. En **Settings > Deploy > Healthcheck Path**, configure `/health`.
3. Use un timeout de healthcheck de 120 segundos para permitir el arranque en frío.
4. En Free, use la política de reinicio **On Failure**, con un máximo de 10 reintentos.

## 5. Verificación

Compruebe:

```text
GET https://SU-DOMINIO/health
```

Debe responder:

```json
{"status":"healthy"}
```

El usuario administrador se crea únicamente la primera vez. Después del primer acceso,
mantenga la contraseña fuera del repositorio y de los logs.

## Consideraciones de SQLite

- El archivo `/data/muyu.db` sobrevive reinicios y nuevos despliegues gracias al volumen.
- No aumente el número de réplicas por encima de una.
- Realice copias de seguridad periódicas del archivo SQLite y del directorio de evidencias.
- `EnsureCreated` prepara una instalación nueva. Los futuros cambios de esquema deberán
  gestionarse con migraciones de Entity Framework Core.
