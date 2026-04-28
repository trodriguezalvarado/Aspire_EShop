import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard'; // Adjust path if needed
import { CatalogoAdminComponent } from './Components/catalogo-admin/catalogo-admin.component';
import { InventarioCrudComponent } from './Components/inventario-crud/inventario-crud.component';
import { DashboardHomeComponent } from './Components/dashboard-home/dashboard-home.component';

const routes: Routes = [
  { path: 'home', component: DashboardHomeComponent, canActivate: [AuthGuard] },
  { path: 'catalogo', component: CatalogoAdminComponent, canActivate: [AuthGuard], data: { roles: ['Product_Manager'] } },
  { path: 'inventario', component: InventarioCrudComponent, canActivate: [AuthGuard], data: { roles: ['Catalog_Manager'] } },
  { path: '', redirectTo: 'home', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
