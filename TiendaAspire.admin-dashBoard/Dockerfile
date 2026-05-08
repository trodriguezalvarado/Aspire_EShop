# Stage 1: Build the Angular app
FROM node:18-alpine as build
WORKDIR /app

# Add this line to accept the variable
ARG CONFIGURATION=development 
ARG BASE_HREF=/admin/

# Copy package files and install dependencies
COPY TiendaAspire.admin-dashBoard/package*.json ./
RUN npm install --legacy-peer-deps

# Copy the rest of the code and build for production
COPY TiendaAspire.admin-dashBoard/ .
RUN npx ng build --configuration ${CONFIGURATION} --base-href ${BASE_HREF}

# Stage 2: Serve the app with Nginx
FROM nginx:alpine

# 1. Borramos el contenido por defecto
RUN rm -rf /usr/share/nginx/html/*

# 2. Creamos la subcarpeta física 'admin'
RUN mkdir -p /usr/share/nginx/html/admin

# Copy the build output from the first stage
# Note: Check your 'angular.json' to confirm the output path (usually dist/project-name)
COPY --from=build /app/dist/tienda-aspire.admin-dash-board /usr/share/nginx/html/admin

# Copy a custom Nginx config to handle Angular routing
COPY TiendaAspire.admin-dashBoard/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
