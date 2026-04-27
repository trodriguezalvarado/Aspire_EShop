import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventarioCrudComponent } from './inventario-crud.component';

describe('InventarioCrudComponent', () => {
  let component: InventarioCrudComponent;
  let fixture: ComponentFixture<InventarioCrudComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [InventarioCrudComponent]
    });
    fixture = TestBed.createComponent(InventarioCrudComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
