# 🛒 Aspire E-Shop: Cloud-Native Microservices Architecture

Este proyecto es una implementación avanzada de una tienda virtual basada en una arquitectura de microservicios distribuidos, utilizando **.NET Aspire** y **Dapr (Distributed Application Runtime)** . Está diseñada bajo estándares empresariales para ser altamente escalable, segura, tolerante a fallos y optimizada para entornos de contenedores independientes gestionados por **Docker Compose** .

---

## 🚀 Arquitectura del Sistema

La solución está construida sobre una red interna desacoplada y securizada, donde los componentes fundamentales operan de forma independiente y asíncrona :

*   **Catalog API**: Servicio crítico de gestión de productos con persistencia de datos en **SQL Server**, optimizado con una capa de caché de estado y resiliencia distribuida respaldada por **Dapr State Management (Redis)** .
*   **Inventory API**: Servicio independiente para el control de stock físico que procesa actualizaciones masivas y sincronizaciones de manera asíncrona mediante un bus de mensajería desacoplado impulsado por **Dapr Pub/Sub (RabbitMQ)** .
*   **Admin Dashboard**: Interfaz administrativa SPA desarrollada en **Angular** que interactúa con las APIs a través del Gateway seguro .
*   **Store Front**: Interfaz de cliente pública de alto rendimiento desarrollada en **Blazor WebAssembly** .

---

## 🛠️ Stack Tecnológico Enterprise

*   **Orquestador de Desarrollo**: .NET Aspire & AppHost Dashboard .
*   **Abstracción de Microservicios**: Dapr Runtime v1.14.4 (Sidecars distribuidos) .
*   **Frameworks de Aplicación**: .NET 8, ASP.NET Core APIs, Angular 17 & Blazor WASM .
*   **Gestión de Mensajería**: RabbitMQ (Mensajería asíncrona desacoplada por eventos) .
*   **Persistencia de Datos**: SQL Server & Redis Distributed Cache .
*   **Seguridad e Identidad**: Keycloak (Servidor OIDC / OAuth2 robusto para protección de tokens JWT) .
*   **Gateway / Proxy de Entrada**: Nginx Reverse Proxy (Único punto de entrada seguro con terminación SSL en producción) .
*   **Infraestructura de Despliegue**: Docker & Docker Compose (Cascadas multi-entorno) .

---

## 💡 Desafíos Técnicos Resueltos

Esta sección destaca la ingeniería y resolución de problemas reales implementados durante el desarrollo del proyecto:

*   **Aislamiento y Abstracción con Dapr**: Migración del acoplamiento directo de infraestructura hacia sidecars independientes de Dapr . Se eliminaron por completo las dependencias de SDKs específicos en el código C#, delegando la gestión de colas (RabbitMQ) y caché (Redis) al plano de control de Dapr de manera 100% transparente .
*   **Estabilización de Ciclos de Vida (Startup Race Conditions)**: Resolución de errores de temporización en contenedores (`501 EOF` loops) mediante el desacoplamiento de los sidecars hacia una red de puente independiente (*Independent Bridge Network Strategy*) . Se implementaron pruebas de salud rigurosas (`rabbitmq-diagnostics check_running`), bloqueando el arranque de los microservicios hasta que los brokers de datos estén 100% listos .
*   **Inyección Seguridad Multi-Entorno**: Automatización de credenciales sin edición manual de archivos. Se diseñó un flujo unificado donde los secretos se extraen de la máquina local o del servidor físico (`.env`), procesando dinámicamente las plantillas unificadas mediante scripts adaptativos en **PowerShell** (Desarrollo Windows) y comandos **envsubst** (Producción Linux), manteniendo las claves confidenciales protegidas y excluidas de Git .
*   **Gestión de Redes y NAT Loopback**: Implementación de un Gateway centralizado en Nginx que maneja la reescritura de rutas (`rewrite`) y cabeceras proxy (`X-Forwarded-Proto`) . Se resolvió la imposibilidad de comunicación por bucle invertido (*NAT Loopback Reflection*) en entornos de contenedores aislados mediante la inyección nativa de mapas de host internos (`extra_hosts`).

---

## 📦 Cómo ejecutar localmente

Siga estos pasos estructurados para levantar el clúster completo de microservicios de forma local en su máquina de desarrollo utilizando las plantillas unificadas de Dapr :

### 1. Clonar el repositorio
Asegúrese de estar en su directorio de trabajo local y clone la rama del proyecto :
```bash
git clone https://github.com/trodriguezalvarado/Aspire_EShop
cd Aspire_EShop/
```

### 2. Verificar Prerrequisitos del Sistema
*   Asegúrese de tener **Docker Desktop** iniciado y corriendo en su sistema.
*   **Crear un alias DNS local:** Abra su archivo `hosts` de Windows (`C:\Windows\System32\drivers\etc\hosts`) como Administrador y añada la siguiente línea para habilitar el enrutamiento interno :
    ```text
    127.0.0.1   tiendalocal
    ```

### 3. Configurar el Admin Dashboard (Angular)
Navegue hasta la carpeta de configuraciones de entorno `TiendaAspire.admin-dashBoard/src/app/environments/` . Abra el archivo `environment.ts` y asegúrese de que apunte a su alias de desarrollo local :
```typescript
keycloakUrl: 'http://tiendalocal/auth'
```

### 4. Crear el archivo `.env` de Desarrollo Local
Cree un archivo llamado `.env` en la raíz de la solución y configure las siguientes variables básicas . Asegúrese de asignar su alias `tiendalocal` a la propiedad `HOSTNAME`, y deje las contraseñas de Rabbit y Redis vacías para desarrollo local :

```env
SQL_SA_PASSWORD=SU_CONTRASEÑA_PARA_SQL
KEYCLOAK_ADMIN_USER=admin
KEYCLOAK_ADMIN_PASSWORD=admin
HOSTNAME=tiendalocal
KC_HOSTNAME=\${HOSTNAME}
KC_HTTP_RELATIVE_PATH=/auth
KC_HOSTNAME_STRICT=false
KC_PROXY_HEADERS=xforwarded
KEYCLOAK_URL=http://\${HOSTNAME}/auth/
Authentication__Schemes__Bearer__Authority=http://${HOSTNAME}/auth/realms/TiendaRealm

# Infraestructura Dapr (Local por defecto)
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
REDIS_STATIC_PASSWORD=

# Cadenas de conexión para las APIs
# Nota: Aquí usamos el nombre del servicio en Docker (sql-server, redis, etc.)
CONNECTION_SQL_CATALOGO=Server=sql-server;Database=catalogdb;User Id=sa;Password=${SQL_SA_PASSWORD};TrustServerCertificate=True
CONNECTION_SQL_INVENTARIO=Server=sql-server;Database=inventorydb;User Id=sa;Password=${SQL_SA_PASSWORD};TrustServerCertificate=True
CONNECTION_RABBITMQ=amqp://${RABBITMQ_DEFAULT_USER}:${RABBITMQ_DEFAULT_PASS}@rabbitmq:5672
```

### 5. Generar Componentes de Dapr e Iniciar la Solución (PowerShell)
Para evitar la edición manual de archivos e impedir fugas de credenciales, abra una consola de **PowerShell** en la raíz del proyecto y ejecute este comando unificado en una sola línea . El script procesará automáticamente sus plantillas de Dapr e iniciará los contenedores de forma simétrica a producción :

```powershell
New-Item -ItemType Directory -Force -Path ".\dapr\components-local"; Get-Content .env | ForEach-Object { if (\(_ -match '^([^=]+)=(.*)\)') { [System.Environment]::SetEnvironmentVariable(\(Matches[1].Trim(),\)Matches[2].Trim(), "Process") } }; \(p = Get-Content .\dapr\components-templates\pubsub.yaml -Raw; [regex]::Matches(\)p, '\$\{([^}]+)\}') | ForEach-Object { \(v = [System.Environment]::GetEnvironmentVariable(\)_.Groups[1].Value, "Process"); if (v -eq null) { v = "" ; p = p.Replace(_.Value, v) ; p | Set-Content .\dapr\components-local\pubsub.yaml; \(s = Get-Content .\dapr\components-templates\statestore.yaml -Raw; [regex]::Matches(\)s, '\$\{([^}]+)\}') | ForEach-Object { \(v = [System.Environment]::GetEnvironmentVariable(\)_.Groups[1].Value, "Process"); if (v -eq null) { v = "" ; s = s.Replace(_.Value, v) ; s | Set-Content .\dapr\components-local\statestore.yaml; docker compose down; docker compose up -d
```

### 6. Acceso al Sistema
Una vez completada la inicialización interna de los sidecars de Dapr, acceda a las plataformas locales :
*   **Dashboard administrativo de keycloak:** `http://localhost/auth/admin`(Inicie sesión con las credenciales utilizadas en el archivo .env para las variables KEYCLOAK_ADMIN_USER y KEYCLOAK_ADMIN_PASSWORD=admin, acceder a TiendaRealm, cliente, admin-dashboard y sustituir localhost:5001 por el alias adicionado, en el caso de ejemplo tiendalocal)
*   **Tienda Web (Blazor):** `http://tiendalocal`
*   **Dashboard Administrativo (Angular):** `http://tiendalocal` (Inicie sesión con las credenciales precargadas: Usuario: `tomas` | Contraseña: `t34m0`) .
*   **Consola de RabbitMQ:** `http://localhost:15672` (Usuario: `guest` | Clave: `guest`) .

*Nota: La carpeta `dapr/components-local/` contiene contraseñas locales generadas automáticamente y se encuentra protegida dentro del archivo `.gitignore` para impedir que se suba accidentalmente al control de versiones público .*