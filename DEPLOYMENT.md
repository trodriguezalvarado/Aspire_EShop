#!/bin/bash

### 0. Preparar el Sistema
# Actualizar los repositorios e instalar Git
sudo apt update && sudo apt install -y git

### 1. Clone your repository
git clone https://github.com/trodriguezalvarado/Aspire_EShop.git
cd Aspire_Eshop/

### 2. Ejecutar el script setup.sh para actualizar el sistema, instalar dependencias iniciales, instalar Docker y Docker Compose, configurar
# SWAP (4GB) y el Firewall ports (80, 443, 22)
sudo ./setup.sh

# Importante, al finalizar el setup.sh se debe cerrar la sesion y volver a iniciar, luego de iniciar es necesario moverse a la carpeta del repositorio antes de crear el archivo .env o ejecutar docker
cd Aspire_EShop

### 3. Configurar el Admin Dashboard (Mandatorio)
# Navegar hasta la siguiente carpeta y modificar el archivo environment.prod.ts
# Localizar keycloakUrl: '...'
# Cambiar a keycloakUrl: 'https://Tu nombre de dominio o dirección Ip (Nombre de dominio recomendado)/auth
 nano TiendaAspire.admin-dashBoard/src/environments/environment.prod.ts
 
 

### 4. Crear el archivo .env 
Cree un archivo llamado `.env` en la raíz de la solución (`nano .env`) y configure sus variables de entorno reales. Este archivo actúa como la única fuente de verdad para todo el sistema, incluyendo las credenciales secretas que Docker Compose inyectará de forma automática en los componentes de Dapr:

nano .env

```bash
# === CREDENCIALES ADMINISTRATIVAS ===
KEYCLOAK_ADMIN_USER=admin
KEYCLOAK_ADMIN_PASSWORD=admin_password_seguro

# === CONFIGURACIÓN DE RED Y DOMINIO ===
# Nombre completo de DNS (ej. tutienda.duckdns.org) o Dirección IP
HOSTNAME=nombre de dns completo del host
KC_HOSTNAME=${HOSTNAME}
KC_HTTP_RELATIVE_PATH=/auth
KC_HOSTNAME_STRICT=false
KC_PROXY_HEADERS=xforwarded
KEYCLOAK_URL=https://\${HOSTNAME}/auth/

# === AUTENTICACIÓN Y VALIDACIÓN DE APIS ===
Authentication__Schemes__Bearer__Authority=https://${HOSTNAME}/auth/realms/TiendaRealm

# === INFRAESTRUCTURA DE DATOS (DAPR / COMPONENTES) ===
# Estas credenciales serán inyectadas automáticamente por Docker Compose
# en los archivos 'pubsub.yaml' y 'statestore.yaml' en tiempo de ejecución.
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=rabbit_password_seguro
REDIS_STATIC_PASSWORD=redis_password_seguro

# === CADENAS DE CONEXIÓN APIS (.NET) ===
SQL_SA_PASSWORD=sql_password_seguro
CONNECTION_SQL_CATALOGO="Server=sql-server;Database=catalogdb;User Id=sa;Password=\${SQL_SA_PASSWORD};TrustServerCertificate=True"
CONNECTION_SQL_INVENTARIO="Server=sql-server;Database=inventorydb;User Id=sa;Password=\${SQL_SA_PASSWORD};TrustServerCertificate=True"
CONNECTION_RABBITMQ=amqp://${RABBITMQ_DEFAULT_USER}:${RABBITMQ_DEFAULT_PASS}@rabbitmq:5672
```

### 5. Using https
# IMPORTANTE: Asegúrate de que no haya ningún proceso usando el puerto 80 
# (como un apache o nginx previo) antes de correr esto:
sudo apt update

#en Ubuntu 24.04 la opcion -y me dio problemas 
sudo apt install certbot -y

#Tuve que utilizar esta opcion
sudo apt-get update
sudo apt-get install certbot

# Aqui se debe poner el dominio base a utilizar para generar el certificado, el mismo valor asignado a la variable BASE_DOMAIN
sudo certbot certonly --standalone -d YOUR_DOMAIN.com

### 6. Preparar Persistencia y Permisos (Crucial)
#Ejecutamos estos comandos para evitar que los servicios fallen por falta de acceso al disco, durante la construcción de la solución
#estas carpetas son creadas pero con el ususario root, por lo que al arrancar el sql falla debido a los permisos por lo que se crean
#previamente y se asignan los permisos adecuados:
```bash
# Crear carpetas de datos dentro de la solución

# Asignar permisos para los usuarios internos de los contenedores
# 10001: Usuario mssql | 1000: Usuario keycloak
mkdir -p sql-data keycloak-data redis-data
sudo chown -R 10001:0 ./sql-data
sudo chown -R 1000:1000 ./keycloak-data
sudo chmod -R 770 ./sql-data ./keycloak-data

# Verify that the relative production Dapr folder exists before launching
ls -la ./dapr/components-prod/
```

### 7. Generar Configuraciones en Texto Plano e Iniciar la Aplicación

Para garantizar la seguridad de sus credenciales, los archivos del repositorio son puras plantillas abstractas. Ejecute este bloque de comandos combinado en su terminal de Ubuntu. El sistema creará la carpeta física necesaria, leerá de forma segura las contraseñas de su archivo `.env`, inyectará los datos reales mediante `envsubst` en texto plano y levantará todo el clúster en segundo plano de manera automatizada:

```bash
# 1. Crear la carpeta física de destino para los componentes de producción
mkdir -p dapr/components-prod

# 2. Cargar variables del .env en la consola actual de Linux
export \$(grep -v '^#' .env | xargs)

# 3. Procesar las plantillas e inyectar las credenciales reales en texto plano
envsubst < dapr/components-templates/pubsub.yaml > dapr/components-prod/pubsub.yaml
envsubst < dapr/components-templates/statestore.yaml > dapr/components-prod/statestore.yaml


```

*Nota: La carpeta `dapr/components-prod/` contiene contraseñas en texto plano y ha sido protegida de forma estricta dentro del archivo `.gitignore` para evitar cualquier fuga accidental de secretos hacia el repositorio público de GitHub.*

### 8. Construir y levantar la app usando ambos archivos docker-compose.yml y docker-compose.prod.yml

# Detener cualquier instancia previa y levantar el entorno clúster en producción
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build

# Verify that the "Gateway" is running
docker compose ps

# Luego de construir y levantar la app acceder al url  https://YOUR_DOMAIN.com/auth/admin/, acceder a la consola administrativa de 
# Keycloak acceder a TiendaRealm, Clientes, admin-dashboard
# Cambiar todos los http://localhost:5100 por https://YOUR_DOMAIN.com

# Luego de esto la tienda debe estar disponible en https://YOUR_DOMAIN.com
# El dashboard administrativo debe estar disponible en https://YOUR_DOMAIN.com/admin/
# En esta prueba existe un ususario creado con nombre de ususario tomas y password t34m0, el cual tiene 
# roles de manager de inventario y manager de catalogo por lo que puede realizar ambas operaciones.

# Verificar logs si algo no carga:
# es impresindible incluir el archivo docker-compose.prod.conf ya que que en el archivo docker-compose no se encuentra definido 
# el servicio gateway-nginx ya que se utilizó una estrategia de cascada por lo que si no se incluye se obtendra un error no such service gateway-nginx
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f gateway-nginx

# LIMPIEZA FINAL (Para recuperar espacio en los 60GB):
docker builder prune -f
docker image prune -f


