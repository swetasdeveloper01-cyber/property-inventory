import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../core/models/problem-details';
import { ContactDto } from '../../../core/models/contact.models';
import { ContactApiService } from '../../../core/services/contact-api.service';
import { ContactListPage } from './contact-list-page';

describe('ContactListPage', () => {
  let fixture: ComponentFixture<ContactListPage>;
  let api: {
    getContacts: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  const sample: ContactDto[] = [
    {
      id: 'c1111111-1111-1111-1111-111111111111',
      firstName: 'Carmen',
      lastName: 'Attard',
      phoneNumber: '+356 2123 4567',
      email: 'carmen.attard@example.com'
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
      getContacts: vi.fn(() => of(paged))
    };

    await TestBed.configureTestingModule({
      imports: [ContactListPage],
      providers: [
        { provide: ContactApiService, useValue: api },
        provideRouter([{ path: 'contacts/:id', component: ContactListPage }])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ContactListPage);
    router = TestBed.inject(Router);
  });

  it('renders the contact list heading', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('h1')?.textContent).toContain('Contacts');
  });

  it('calls ContactApiService with default pagination', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    expect(api.getContacts).toHaveBeenCalledWith({
      page: 1,
      pageSize: 10
    });
  });

  it('renders contacts from the API response', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Carmen');
    expect(text).toContain('Attard');
    expect(text).toContain('carmen.attard@example.com');
    expect(text).toContain('+356 2123 4567');
  });

  it('shows loading state while the request is pending', () => {
    api.getContacts.mockReturnValue({
      subscribe: () => ({ unsubscribe() {} })
    });

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-loading')?.textContent).toContain(
      'Loading contacts'
    );
    expect(fixture.nativeElement.querySelector('.data-table')).toBeNull();
  });

  it('shows empty state when API returns no items', async () => {
    api.getContacts.mockReturnValue(
      of({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 })
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-empty')?.textContent).toContain(
      'No contacts match the current filters.'
    );
  });

  it('shows API error state', async () => {
    api.getContacts.mockReturnValue(
      throwError(() => new ApiError(500, { title: 'Server Error', detail: 'boom' }))
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-error')?.textContent).toContain(
      'Unable to load contacts'
    );
  });

  it('apply filters sends correct filter parameters and resets to page 1', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.filterForm.setValue({
      firstName: 'Carmen',
      lastName: 'Attard',
      email: 'carmen',
      phone: '2123'
    });
    component.page.set(3);
    component.applyFilters();
    await fixture.whenStable();

    expect(api.getContacts).toHaveBeenLastCalledWith({
      firstName: 'Carmen',
      lastName: 'Attard',
      email: 'carmen',
      phone: '2123',
      page: 1,
      pageSize: 10
    });
  });

  it('clear filters resets filters and reloads page 1', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    const component = fixture.componentInstance;
    component.filterForm.setValue({
      firstName: 'Carmen',
      lastName: 'Attard',
      email: 'carmen',
      phone: '2123'
    });
    component.applyFilters();
    await fixture.whenStable();

    component.page.set(2);
    component.clearFilters();
    await fixture.whenStable();

    expect(component.filterForm.getRawValue()).toEqual({
      firstName: '',
      lastName: '',
      email: '',
      phone: ''
    });
    expect(api.getContacts).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 10
    });
  });

  it('pagination requests the correct page', async () => {
    api.getContacts.mockReturnValue(
      of({ items: sample, page: 1, pageSize: 10, totalCount: 25, totalPages: 3 })
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const next = fixture.nativeElement.querySelectorAll(
      '.pagination .secondary-button'
    )[1] as HTMLButtonElement;
    next.click();
    await fixture.whenStable();

    expect(api.getContacts).toHaveBeenLastCalledWith({
      page: 2,
      pageSize: 10
    });
  });

  it('edit action navigates to the correct contact', async () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const link = fixture.debugElement.query(By.css('.action-link'));
    expect(link.attributes['href']).toBe('/contacts/c1111111-1111-1111-1111-111111111111');

    link.nativeElement.click();
    await fixture.whenStable();

    expect(navigateSpy).toHaveBeenCalled();
  });
});
