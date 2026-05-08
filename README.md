# 🛒 Aspire E-Shop: Microservices Architecture

Este proyecto es una implementación de una tienda basada en microservicios utilizando **.NET Aspire**, diseñada para ser escalable, segura y eficiente en entornos de contenedores **Docker**.

## 🚀 Arquitectura del Sistema
La solución se compone de microservicios desacoplados que se comunican a través de una red interna gestionada por un Gateway de Nginx.

*   **Catalog API**: Gestión de productos con persistencia en SQL Server y caché en Redis.
*   **Inventory API**: Control de stock y sincronización de inventario.
*   **Admin Dashboard**: Interfaz administrativa desarrollada en Angular.
*   **Store Front**: Interfaz de cliente desarrollada en Blazor.

## 🛠️ Stack Tecnológico
*   **Backend**: .NET 8 / .NET Aspire.
*   **Frontend**: Angular 17 / Blazor.
*   **Infraestructura**: Docker & Docker Compose.
*   **Seguridad**: Keycloak (OIDC / OAuth2).
*   **Bases de Datos**: SQL Server, Redis (Caché).
*   **Mensajería**: RabbitMQ.
*   **Gateway/Proxy**: Nginx.

## 💡 Desafíos Técnicos Resueltos
Esta sección destaca la resolución de problemas reales durante el desarrollo:

*   **Gestión de Redes en Docker**: Implementación de un **Reverse Proxy con Nginx** como Gateway único, gestionando la reescritura de rutas (`rewrite`) y centralizando el tráfico para evitar problemas de CORS.
*   **Seguridad Distribuida**: Configuración de **Keycloak** en contenedores, resolviendo conflictos de validación de tokens entre contextos de red (`localhost` vs `internal-network`) mediante ajustes de emisor (`Issuer`).
*   **Persistencia y Resiliencia**: Estrategias de migración automática con EF Core y políticas de reintentos mediante **Polly Resilience Pipelines**.
*   **Optimización**: Despliegue optimizado para entornos con recursos limitados.

## 📦 Cómo ejecutar localmente
1. Clonar el repositorio.
2. Asegurarse de tener Docker Desktop iniciado.
3. Crear un alias en el archivo host o dns local
4. Configurar el admin dashboard
  - Moverse a la carpeta TiendaAspire.admin-dashBoard/src/environments/
  - Modificar el archivo environment.ts
  - Localizar el archivo keycloakUrl: '...'
  - cambiar por keycloakUrl: 'htttp://ALIAS/auth' (donde ALIAS es el mismo creado en el paso 3)
  - Crear el archivo .env, en la raiz de la soluci�n, en el archivo DEPLOMENT.md hay una secci�n con las variables a definir, en el caso de la varable HOSTNAME debe tener como valor el ALIAS definido.
3. Ejecutar el comando:
   ```bash
   docker-compose up -d
   ```
4. Acceder al dashboard administrativo en `http://localhost:5100`.

