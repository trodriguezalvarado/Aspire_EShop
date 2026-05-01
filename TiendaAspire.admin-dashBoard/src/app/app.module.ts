import { APP_INITIALIZER, NgModule } from '@angular/core';
import { KeycloakAngularModule, KeycloakService } from 'keycloak-angular';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';
import { RouterModule, Routes } from '@angular/router';
import { AppComponent } from './app.component';
import { ListaComponent } from './catalogo/activar/indice/lista/lista.component';
import { FormsModule } from '@angular/forms';
import { CatalogoAdminComponent } from './Components/catalogo-admin/catalogo-admin.component';
import { InventarioCrudComponent } from './Components/inventario-crud/inventario-crud.component';
import { DashboardHomeComponent } from './Components/dashboard-home/dashboard-home.component';


export function initializeKeycloak(keycloak: KeycloakService) {
  return () =>
    keycloak.init({
      config: {
        url: 'https://tutienda.duckdns.org/auth/',
        realm: 'TiendaRealm',
        clientId: 'admin-dashboard'
      },
      initOptions: {
        onLoad: 'login-required',
        checkLoginIframe: false,
        silentCheckSsoRedirectUri:
        window.location.origin + '/assets/silent-check-sso.html'
      },
      enableBearerInterceptor: true,
      bearerExcludedUrls: ['/assets']
    });
}

@NgModule({
  declarations: [
    AppComponent,
    ListaComponent,
    CatalogoAdminComponent,
    InventarioCrudComponent,
    DashboardHomeComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule,
    KeycloakAngularModule,
    RouterModule
  ],
  providers: [{
    provide: APP_INITIALIZER,
    useFactory: initializeKeycloak,
    multi: true,
    deps: [KeycloakService]
  },
    KeycloakService],
  bootstrap: [AppComponent]
})
export class AppModule { }
