export const environment = {
  production: false,
  keycloakUrl: 'http://tiendalocal/auth', // Overwritten for the Docker network domain mesh
  catalogoUrl: '/api/catalogo',           // Replicated so it doesn't become undefined
  inventarioUrl: '/api/inventario'
};
