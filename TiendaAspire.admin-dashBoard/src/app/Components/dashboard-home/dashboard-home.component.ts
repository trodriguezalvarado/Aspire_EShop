import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { KeycloakService } from 'keycloak-angular';

@Component({
  selector: 'app-dashboard-home',
  templateUrl: './dashboard-home.component.html',
  styleUrls: ['./dashboard-home.component.css']
})
export class DashboardHomeComponent implements OnInit {
  userRoles: string[] = [];

  constructor(private keycloak: KeycloakService) { }

  ngOnInit() {
    // Get real roles from Keycloak
    this.userRoles = this.keycloak.getUserRoles();
    //const tokenDetails: any = this.keycloak.getKeycloakInstance().tokenParsed;
    //this.userRoles = tokenDetails['role'] || []; 
  }

  hasRole(role: string): boolean {
    return this.userRoles.includes(role);
  }
}
