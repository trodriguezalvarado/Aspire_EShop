#!/bin/bash

# 1. Actualizar el sistema
echo "--- Actualizando sistema ---"
sudo apt update && sudo apt upgrade -y

# 2. Crear archivo SWAP de 4GB (Vital para SQL Server/Keycloak con 8GB RAM)
echo "--- Configurando SWAP de 4GB ---"
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap defaults 0 0' | sudo tee -a /etc/fstab

# 3. Instalar dependencias iniciales
echo "--- Instalando dependencias ---"
sudo apt install -y ca-certificates curl gnupg lsb-release ufw

# 4. Instalar Docker y Docker Compose
echo "--- Instalando Docker ---"
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# 5. Configurar permisos de usuario para Docker
sudo usermod -aG docker $USER

# 6. Configurar Firewall (UFW)
echo "--- Configurando Firewall ---"
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow ssh             # Puerto 22 para ti
sudo ufw allow http            # Puerto 80 para Nginx
sudo ufw allow https           # Puerto 443 para Nginx (Producción)
sudo ufw allow 1434/tcp        # Opcional: Solo si quieres entrar por SSMS desde fuera
sudo ufw --force enable

# 7. Optimización de Swappiness (Mejora rendimiento con SQL Server)
echo "vm.swappiness=10" | sudo tee -a /etc/sysctl.conf
sudo sysctl -p

echo "--- ¡Configuración completada! ---"
echo "IMPORTANTE: Cierra sesión y vuelve a entrar para que los permisos de Docker funcionen."
