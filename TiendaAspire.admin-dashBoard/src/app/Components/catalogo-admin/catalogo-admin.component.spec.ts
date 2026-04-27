import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CatalogoAdminComponent } from './catalogo-admin.component';

describe('CatalogoAdminComponent', () => {
  let component: CatalogoAdminComponent;
  let fixture: ComponentFixture<CatalogoAdminComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [CatalogoAdminComponent]
    });
    fixture = TestBed.createComponent(CatalogoAdminComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
