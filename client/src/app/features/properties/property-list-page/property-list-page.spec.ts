import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../core/models/problem-details';
import { PropertyDto } from '../../../core/models/property.models';
import { PropertyApiService } from '../../../core/services/property-api.service';
import { PropertyListPage } from './property-list-page';

describe('PropertyListPage', () => {
  let fixture: ComponentFixture<PropertyListPage>;
  let api: {
    getProperties: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  const sample: PropertyDto[] = [
    {
      id: 'a1111111-1111-1111-1111-111111111111',
      name: 'Maisonette',
      address: '12 High Street',
      price: 130000,
      currency: 'EUR',
      dateOfRegistration: '2020-03-15'
    }
  ];

  const paged = {
    items: sample,
    page: 1,
    pageSize: 10,
    totalCount: 1,
    totalPages: 1
  };

  beforeEach(async () => {
    api = {
      getProperties: vi.fn(() => of(paged))
    };

    await TestBed.configureTestingModule({
      imports: [PropertyListPage],
      providers: [
        { provide: PropertyApiService, useValue: api },
        provideRouter([{ path: 'properties/:id', component: PropertyListPage }])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyListPage);
    router = TestBed.inject(Router);
  });

  it('renders the property list heading', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('h1')?.textContent).toContain('Properties');
  });

  it('calls PropertyApiService with default pagination', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    expect(api.getProperties).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10
    });
  });

  it('renders properties from the API response', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Maisonette');
    expect(text).toContain('12 High Street');
    expect(text).toContain('130,000.00');
    expect(text).toContain('15 Mar 2020');
  });

  it('shows loading state while the request is pending', () => {
    api.getProperties.mockReturnValue({
      subscribe: () => ({ unsubscribe() {} })
    });

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-loading')?.textContent).toContain(
      'Loading properties'
    );
    expect(fixture.nativeElement.querySelector('.data-table')).toBeNull();
  });

  it('shows empty state when API returns no items', async () => {
    api.getProperties.mockReturnValue(
      of({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 })
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-empty')?.textContent).toContain(
      'No properties match the current filters.'
    );
  });

  it('shows API error state', async () => {
    api.getProperties.mockReturnValue(
      throwError(() => new ApiError(500, { title: 'Server Error', detail: 'boom' }))
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-error')?.textContent).toContain(
      'Unable to load properties'
    );
  });

  it('apply filters sends correct filter parameters and resets to page 1', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.filterForm.setValue({
      name: 'Maison',
      address: 'High',
      minPrice: '100000',
      maxPrice: '200000'
    });
    component.page.set(3);
    component.applyFilters();
    await fixture.whenStable();

    expect(api.getProperties).toHaveBeenLastCalledWith({
      name: 'Maison',
      address: 'High',
      minPrice: 100000,
      maxPrice: 200000,
      page: 1,
      pageSize: 10
    });
  });

  it('clear filters resets filters and reloads page 1', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    component.filterForm.setValue({
      name: 'Maison',
      address: 'High',
      minPrice: '100000',
      maxPrice: '200000'
    });
    component.applyFilters();
    await fixture.whenStable();

    component.page.set(2);
    component.clearFilters();
    await fixture.whenStable();

    expect(component.filterForm.getRawValue()).toEqual({
      name: '',
      address: '',
      minPrice: '',
      maxPrice: ''
    });
    expect(api.getProperties).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 10
    });
  });

  it('pagination requests the correct page', async () => {
    api.getProperties.mockReturnValue(
      of({ items: sample, page: 1, pageSize: 10, totalCount: 25, totalPages: 3 })
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const next = fixture.nativeElement.querySelectorAll('.pagination .secondary-button')[1] as HTMLButtonElement;
    next.click();
    await fixture.whenStable();

    expect(api.getProperties).toHaveBeenLastCalledWith({
      page: 2,
      pageSize: 10
    });
  });

  it('edit action navigates to the correct property', async () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const link = fixture.debugElement.query(By.css('.action-link'));
    expect(link.attributes['href']).toBe('/properties/a1111111-1111-1111-1111-111111111111');

    link.nativeElement.click();
    await fixture.whenStable();

    expect(navigateSpy).toHaveBeenCalled();
  });
});
