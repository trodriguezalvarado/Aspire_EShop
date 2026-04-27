import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { KeycloakAuthGuard, KeycloakService } from 'keycloak-angular';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard extends KeycloakAuthGuard {
  constructor(
    protected override readonly router: Router,
    protected readonly keycloak: KeycloakService
  ) {
    super(router, keycloak);
  }

  public async isAccessAllowed(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ) {
    // 1. Forzar login si no está autenticado
    if (!this.authenticated) {
      await this.keycloak.login({
        redirectUri: window.location.origin + state.url
      });
    }

    // 2. Obtener roles requeridos desde la ruta (configurados en app-routing.module.ts)
    const requiredRoles = route.data['roles'];

    // 3. Si la ruta no pide roles específicos, permitir acceso
    if (!(requiredRoles instanceof Array) || requiredRoles.length === 0) {
      return true;
    }

    // 4. Verificar si el usuario tiene TODOS los roles requeridos
    return requiredRoles.every((role) => this.roles.includes(role));
  }
}

