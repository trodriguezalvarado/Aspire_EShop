import { TestBed } from '@angular/core/testing';

import { InvetarioserviceService } from './invetarioservice.service';

describe('InvetarioserviceService', () => {
  let service: InvetarioserviceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(InvetarioserviceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
