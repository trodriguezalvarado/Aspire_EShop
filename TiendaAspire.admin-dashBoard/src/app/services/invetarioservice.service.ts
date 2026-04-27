import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class InventarioService {
  private apiUrl = `${environment.inventarioUrl}/stock`;

  constructor(private http: HttpClient) { }

  listar(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  crear(producto: any): Observable<any> {
    return this.http.post(this.apiUrl, producto);
  }

  actualizar(id: string, nombre: string, cantidad: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, { Nombre: nombre, Cantidad: cantidad });
  }
}
