import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../app/environments/environment';
import { KeycloakService } from 'keycloak-angular';
import { RouterLink } from '@angular/router'

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'TiendaAspire.admin-dashBoard';
  productos: any[] = [];
  username: string = '';

  constructor(private http: HttpClient, private keycloak: KeycloakService) { }

  async ngOnInit() {
    if (await this.keycloak.isLoggedIn()) {
      const profile = await this.keycloak.loadUserProfile();
      this.username = profile.username || 'User';
    }
  }

  logout() {
    this.keycloak.logout(window.location.origin);
  }

  changePassword() {
    // Keycloak provides a standard way to manage accounts
    this.keycloak.getKeycloakInstance().accountManagement();
  }
}
