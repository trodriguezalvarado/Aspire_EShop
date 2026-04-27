import { Component } from '@angular/core';
import { CatalogoService } from '../../services/catalogoservice.service';

@Component({
  selector: 'app-catalogo-admin',
  templateUrl: './catalogo-admin.component.html',
  styleUrls: ['./catalogo-admin.component.css']
})
export class CatalogoAdminComponent {
  productos: any[] = [];
  constructor(private catalogoService: CatalogoService) { }

  ngOnInit() {
    this.cargarPendientes();
  }

  cargarPendientes() {
    this.catalogoService.getPendientes().subscribe({
      next: (data) => this.productos = data,
      error: (err) => console.error('Error al cargar:', err)
    });
  }

  publicar(producto: any) {
    if (!producto.precio || producto.precio <= 0) {
      alert("Por favor, asigne un precio válido.");
      return;
    }

    this.catalogoService.activarProducto(producto.id, producto.precio).subscribe({
      next: () => {
        alert("Producto publicado exitosamente");
        this.cargarPendientes(); // Refresca la lista eliminando el que ya se activó
      },
      error: (err) => alert("Error al activar: " + err.message)
    });
  }
}
