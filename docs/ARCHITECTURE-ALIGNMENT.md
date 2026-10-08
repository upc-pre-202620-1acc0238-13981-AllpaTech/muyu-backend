# Alineación del backend con el reporte y los diagramas C4

## Bounded contexts de la sección 2.6

| Reporte | Módulo backend | Estado |
|---|---|---|
| Gestión y Aprobación de Parcelas | `ParcelManagement` | Implementado |
| Financiamiento de Parcelas | `Agreements` | Implementado |
| Custodia y Pagos Escrow | `Escrow` | Implementado |
| Ejecución Agrícola y Evidencias | `Tracking` | Implementado |
| Monitoreo Climático y Riesgos | `ClimateAlerts` | Implementado |
| Gestión de Usuarios (IAM) | `IAM` | Implementado |

El contexto IAM conserva las cuatro capas `Domain`, `Application`, `Infrastructure` y
`Presentation`. Implementa los registros separados de Agricultor y Comerciante, login JWT,
perfil autenticado, roles, correo/documento únicos, hash de contraseña y los eventos
`UserRegisteredDomainEvent` y `UserAuthenticatedDomainEvent`.

La verificación externa de DNI/RUC queda definida por `IIdentityVerificationProvider`. El adaptador
actual solo valida el formato para el prototipo universitario; no debe describirse como integración
real con RENIEC o SUNAT.

## Cobertura funcional del reporte

La implementación alcanza aproximadamente un 70% verificable del alcance técnico descrito en la
sección 2.6. Todos los bounded contexts exponen rutas versionadas `/api/v1` conservando las rutas
anteriores por compatibilidad. También se implementaron el catálogo público y aprobación de
parcelas, metadatos GPS de evidencias, aprobación/rechazo de hitos y autorización por roles para
las operaciones de escritura principales.

Continúan como alcance futuro la validación espacial contra un proveedor satelital, firma digital
bilateral de acuerdos, OAuth social, integración real RENIEC/SUNAT, broker de mensajes, pasarela
bancaria real y envío de notificaciones por WhatsApp/SMS.

## Diferencias con los diagramas C4 entregados

Los diagramas representan una arquitectura objetivo distribuida, mientras que este repositorio es
un monolito modular desplegado para demostración. Las siguientes diferencias deben declararse en el
reporte o corregirse en una futura iteración:

| Diagrama C4 | Implementación actual |
|---|---|
| Microservicios/pods independientes en AWS EKS | Un contenedor .NET 8 en Railway |
| Bases MySQL separadas en AWS RDS | Una base SQLite persistida en un volumen |
| Evidencias en AWS S3 | Archivos en el volumen local configurado |
| OAuth con Google/Facebook | Autenticación propia por correo/contraseña y JWT |
| Proveedor RENIEC/SUNAT | Puerto preparado con adaptador local de demostración |
| Notification Dispatcher independiente | No existe como bounded context independiente |
| Pasarela bancaria real | Adaptador de pago de demostración |

El System Context es válido como vista del ecosistema objetivo. En cambio, Container y Deployment
no describen literalmente el backend desplegado actualmente. Para una entrega coherente hay dos
opciones: rotularlos como **arquitectura objetivo** o generar versiones **as-built** con Railway,
monolito modular, SQLite y volumen persistente.
