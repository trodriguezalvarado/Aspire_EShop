import { Component, OnInit } from '@angular/core';
import { InventarioService } from '../../services/invetarioservice.service'

@Component({
  selector: 'app-inventario-crud',
  templateUrl: './inventario-crud.component.html',
  styleUrls: ['./inventario-crud.component.css']
})
export class InventarioCrudComponent implements OnInit {
  productos: any[] = [];
  nuevoProd = { nombre: '', cantidad: 0 };

  constructor(private inventarioService: InventarioService) { }

  ngOnInit() { this.cargar(); }

  cargar() {
    this.inventarioService.listar().subscribe(data => this.productos = data);
  }

  guardarNuevo() {
    this.inventarioService.crear(this.nuevoProd).subscribe(() => {
      this.cargar();
      this.nuevoProd = { nombre: '', cantidad: 0 };
    });
  }

  actualizarStock(p: any) {
    this.inventarioService.actualizar(p.codigoUnico, p.nombre, p.stock).subscribe(() => {
      alert("Stock actualizado y notificado al catálogo");
    });
  }
}

