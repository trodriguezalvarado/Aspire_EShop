import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../app/environments/environment';

@Injectable({
  providedIn: 'root' // Esto hace que el servicio esté disponible en toda la app
})
export class CatalogoService {
  private baseUrl = `${environment.catalogoUrl}/catalogo`;

  constructor(private http: HttpClient) { }

  // Obtener los productos que no tienen precio aún
  getPendientes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/pendientes`);
  }

  // Enviar el precio para activar el producto
  activarProducto(id: string, precio: number): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}/activar`, { precio });
  }
}
