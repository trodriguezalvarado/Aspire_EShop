import { TestBed } from '@angular/core/testing';

import { CatalogoserviceService } from './catalogoservice.service';

describe('CatalogoserviceService', () => {
  let service: CatalogoserviceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CatalogoserviceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
