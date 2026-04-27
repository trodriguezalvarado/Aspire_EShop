// proxy.conf.js
const { env } = require('process');

// Aspire inyecta las URLs en variables de entorno con este formato
const targetCatalogo = env.services__catalogoservice__http__0 || env.services__catalogoservice__https__0;
const targetInventario = env.services__inventarioservice__http__0 || env.services__inventarioservice__https__0;

const PROXY_CONFIG = {
  "/api/catalogo": {
    "target": targetCatalogo,
    "secure": false,
    "pathRewrite": { "^/api/catalogo": "" }
  },
  "/api/inventario": {
    "target": targetInventario,
    "secure": false,
    "pathRewrite": { "^/api/inventario": "" }
  }
};

module.exports = PROXY_CONFIG;
