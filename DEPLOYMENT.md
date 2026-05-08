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
nano .env
# [Adicione sus password reales aqui y el domininio o dirreccion IP del servidor]
#Estas son las variables necesarias
KEYCLOAK_ADMIN_USER=admin
KEYCLOAK_ADMIN_PASSWORD=admin

# Nombre completo de dns o direccion ip del host, preferentemente nombre de dns
# En las pruebas se creo el host tutienda.duckdns.org, y se utilizo duckdns para crear este host, una variante
# facily rapida para una prueba de despliegue.
# En caso de utilizar la direccion ip, se debe generar un certificado self signed ya que Cerbot no acepta direcciones ip
HOSTNAME =Tu nombre de dominio o dirección Ip, Nombre de dominio recomendado

KC_HOSTNAME=${HOSTNAME}
KC_HTTP_RELATIVE_PATH=/auth
KC_HOSTNAME_STRICT=false
KC_HOSTNAME_STRICT_HTTPS=false
# El proxy-headers debe ser xforwarded
KC_PROXY_HEADERS=xforwarded

# Este valor es el del dominio que va a ser utilizado o la dirección ip, es recomendable usar un dominio
KEYCLOAK_URL=https://${HOSTNAME}/auth/

# Authority para validación de tokens en las APIs
# omitir el /auth/ al final es la cuasa más común de dashboards vacios.
Authentication__Schemes__Bearer__Authority=https://${HOSTNAME}/auth/realms/TiendaRealm

# Credenciales SQL Server
SQL_SA_PASSWORD=T34m0W2kRalperios*

# Credenciales RabbitMQ
RABBITMQ_USER=guest
RABBITMQ_PASS=guest

# Cadenas de conexión para las APIs
# Nota: Aquí usamos el nombre del servicio en Docker (sql-server, redis, etc.)
CONNECTION_SQL_CATALOGO="Server=sql-server;Database=catalogdb;User Id=sa;Password=T34m0W2kRalperios*;TrustServerCertificate=True"
CONNECTION_SQL_INVENTARIO="Server=sql-server;Database=inventorydb;User Id=sa;Password=T34m0W2kRalperios*;TrustServerCertificate=True"
CONNECTION_RABBITMQ=amqp://guest:guest@rabbitmq:5672

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
```

### 7. Construir y levantar la app usando ambos archivos docker-compose.yml y docker-compose.prod.yml
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


