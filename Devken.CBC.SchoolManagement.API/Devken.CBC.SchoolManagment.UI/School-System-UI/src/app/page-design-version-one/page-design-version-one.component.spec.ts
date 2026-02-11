import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PageDesignVersionOneComponent } from './page-design-version-one.component';

describe('PageDesignVersionOneComponent', () => {
  let component: PageDesignVersionOneComponent;
  let fixture: ComponentFixture<PageDesignVersionOneComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PageDesignVersionOneComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PageDesignVersionOneComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
