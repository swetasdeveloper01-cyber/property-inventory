import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ContactDto } from '../../../core/models/contact.models';
import { OwnershipDto } from '../../../core/models/ownership.models';
import { ApiError } from '../../../core/models/problem-details';
import { ContactApiService } from '../../../core/services/contact-api.service';
import { OwnershipApiService } from '../../../core/services/ownership-api.service';
import { PropertyOwnershipSection } from './property-ownership-section';

describe('PropertyOwnershipSection', () => {
  let fixture: ComponentFixture<PropertyOwnershipSection>;
  let ownershipApi: {
    getOwnerships: ReturnType<typeof vi.fn>;
    createOwnership: ReturnType<typeof vi.fn>;
  };
  let contactApi: {
    getContacts: ReturnType<typeof vi.fn>;
  };

  const propertyId = 'a1111111-1111-1111-1111-111111111111';

  const historical: OwnershipDto = {
    id: 'o1111111-1111-1111-1111-111111111111',
    propertyId,
    contactId: 'c1111111-1111-1111-1111-111111111111',
    ownerFirstName: 'Alice',
    ownerLastName: 'Smith',
    ownerEmail: 'alice@example.com',
    effectiveFrom: '2023-01-01',
    effectiveTill: '2024-01-15',
    acquisitionPrice: 100000,
    acquisitionCurrency: 'EUR',
    acquisitionPriceUsd: 108733,
    isCurrent: false
  };

  const current: OwnershipDto = {
    id: 'o2222222-2222-2222-2222-222222222222',
    propertyId,
    contactId: 'c2222222-2222-2222-2222-222222222222',
    ownerFirstName: 'Carmen',
    ownerLastName: 'Attard',
    ownerEmail: 'carmen.attard@example.com',
    effectiveFrom: '2024-01-15',
    effectiveTill: null,
    acquisitionPrice: 120000,
    acquisitionCurrency: 'EUR',
    acquisitionPriceUsd: 130480,
    isCurrent: true
  };

  const contacts: ContactDto[] = [
    {
      id: 'c3333333-3333-3333-3333-333333333333',
      firstName: 'Joe',
      lastName: 'Borg',
      phoneNumber: '+356 1111 2222',
      email: 'joe.borg@example.com'
    },
    {
      id: current.contactId,
      firstName: 'Carmen',
      lastName: 'Attard',
      phoneNumber: '+356 2123 4567',
      email: 'carmen.attard@example.com'
    }
  ];

  async function setup(options?: {
    ownerships?: OwnershipDto[];
    loadError?: unknown;
  }): Promise<void> {
    TestBed.resetTestingModule();

    ownershipApi = {
      getOwnerships: vi.fn(() =>
        options?.loadError
          ? throwError(() => options.loadError)
          : of(options?.ownerships ?? [historical, current])
      ),
      createOwnership: vi.fn(() =>
        of({
          ...current,
          id: 'o-new',
          contactId: contacts[0].id,
          ownerFirstName: 'Joe',
          ownerLastName: 'Borg',
          ownerEmail: contacts[0].email,
          effectiveFrom: '2026-09-10',
          effectiveTill: null,
          acquisitionPrice: 150000,
          acquisitionPriceUsd: 163100
        })
      )
    };

    contactApi = {
      getContacts: vi.fn(() =>
        of({
          items: contacts,
          page: 1,
          pageSize: 100,
          totalCount: contacts.length,
          totalPages: 1
        })
      )
    };

    await TestBed.configureTestingModule({
      imports: [PropertyOwnershipSection],
      providers: [
        { provide: OwnershipApiService, useValue: ownershipApi },
        { provide: ContactApiService, useValue: contactApi }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyOwnershipSection);
    fixture.componentRef.setInput('propertyId', propertyId);
  }

  function openAndFillValidTransfer(): void {
    fixture.componentInstance.openForm();
    fixture.detectChanges();
    fixture.componentInstance.form.setValue({
      contactId: contacts[0].id,
      effectiveFrom: '2026-09-10',
      effectiveTill: '',
      acquisitionPrice: 150000,
      acquisitionCurrency: 'EUR'
    });
  }

  describe('history', () => {
    beforeEach(async () => {
      await setup();
    });

    it('loads ownership history', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      expect(ownershipApi.getOwnerships).toHaveBeenCalledWith(propertyId);
    });

    it('renders ownership records', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Carmen Attard');
      expect(text).toContain('Alice Smith');
    });

    it('identifies current ownership by EffectiveTill = null', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.componentInstance.isCurrent(current)).toBe(true);
      expect(fixture.componentInstance.isCurrent(historical)).toBe(false);
      expect(fixture.nativeElement.querySelector('.current-summary')?.textContent).toContain(
        'Carmen Attard'
      );
      expect(fixture.nativeElement.textContent).toContain('Present');
    });

    it('displays historical ownership date range', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('1 Jan 2023');
      expect(text).toContain('15 Jan 2024');
    });

    it('renders acquisition price and currency', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('120,000.00');
      expect(text).toContain('100,000.00');
    });

    it('renders USD value', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('130,480.00');
      expect(text).toContain('108,733.00');
    });

    it('shows loading state', () => {
      ownershipApi.getOwnerships.mockReturnValue({
        subscribe: () => ({ unsubscribe() {} })
      });

      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-loading')?.textContent).toContain(
        'Loading ownership history'
      );
      expect(fixture.nativeElement.querySelector('.data-table')).toBeNull();
    });

    it('shows empty state', async () => {
      await setup({ ownerships: [] });
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-empty')?.textContent).toContain(
        'No ownership history'
      );
    });

    it('shows API error state', async () => {
      await setup({
        loadError: new ApiError(500, { title: 'Server Error', detail: 'boom' })
      });
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-error')?.textContent).toContain(
        'Unable to load ownership history'
      );
    });
  });

  describe('form', () => {
    beforeEach(async () => {
      await setup();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    });

    it('opens the ownership form', async () => {
      fixture.componentInstance.openForm();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.ownership-form')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('h3')?.textContent).toContain(
        'Transfer Ownership'
      );
    });

    it('makes contact selection available', async () => {
      fixture.componentInstance.openForm();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(contactApi.getContacts).toHaveBeenCalled();
      const options = fixture.nativeElement.querySelectorAll('#ownership-contact option');
      expect(options.length).toBeGreaterThan(1);
    });

    it('requires an owner', () => {
      openAndFillValidTransfer();
      fixture.componentInstance.form.patchValue({ contactId: '' });
      fixture.componentInstance.submit();

      expect(ownershipApi.createOwnership).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('contactId')).toContain('required');
    });

    it('requires Effective From', () => {
      openAndFillValidTransfer();
      fixture.componentInstance.form.patchValue({ effectiveFrom: '' });
      fixture.componentInstance.submit();

      expect(ownershipApi.createOwnership).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('effectiveFrom')).toContain('required');
    });

    it('rejects an invalid date range', () => {
      openAndFillValidTransfer();
      fixture.componentInstance.form.patchValue({
        effectiveFrom: '2026-09-10',
        effectiveTill: '2026-09-01'
      });
      fixture.componentInstance.form.markAllAsTouched();
      fixture.componentInstance.submit();

      expect(ownershipApi.createOwnership).not.toHaveBeenCalled();
      expect(fixture.componentInstance.dateRangeError()).toContain('after Effective From');
    });

    it('rejects an invalid acquisition price', () => {
      openAndFillValidTransfer();
      fixture.componentInstance.form.patchValue({ acquisitionPrice: 0 });
      fixture.componentInstance.submit();

      expect(ownershipApi.createOwnership).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('acquisitionPrice')).toContain(
        'greater than zero'
      );
    });

    it('submits the expected ownership payload without USD', () => {
      openAndFillValidTransfer();
      fixture.componentInstance.submit();

      expect(ownershipApi.createOwnership).toHaveBeenCalledTimes(1);
      expect(ownershipApi.createOwnership).toHaveBeenCalledWith(propertyId, {
        contactId: contacts[0].id,
        effectiveFrom: '2026-09-10',
        effectiveTill: null,
        acquisitionPrice: 150000,
        acquisitionCurrency: 'EUR'
      });

      const payload = ownershipApi.createOwnership.mock.calls[0][1];
      expect(payload).not.toHaveProperty('acquisitionPriceUsd');
    });

    it('disables save while saving', () => {
      ownershipApi.createOwnership.mockReturnValue({
        subscribe: () => ({ unsubscribe() {} })
      });

      openAndFillValidTransfer();
      fixture.componentInstance.submit();
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector(
        'button[type="submit"]'
      ) as HTMLButtonElement;
      expect(button.disabled).toBe(true);
      expect(button.textContent).toContain('Saving');
    });

    it('displays API validation errors', async () => {
      ownershipApi.createOwnership.mockReturnValue(
        throwError(
          () =>
            new ApiError(400, {
              title: 'Validation failed',
              detail: 'One or more validation errors occurred.',
              errors: { ContactId: ['ContactId is required.'] }
            })
        )
      );

      openAndFillValidTransfer();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.banner-error')?.textContent).toContain(
        'validation'
      );
      expect(fixture.componentInstance.controlError('contactId')).toBe('ContactId is required.');
    });
  });

  describe('transfer', () => {
    it('creates a new current owner with one POST, reloads, and shows closed previous till', async () => {
      const closedPrevious: OwnershipDto = {
        ...current,
        effectiveTill: '2026-09-10',
        isCurrent: false
      };
      const newCurrent: OwnershipDto = {
        id: 'o-new',
        propertyId,
        contactId: contacts[0].id,
        ownerFirstName: 'Joe',
        ownerLastName: 'Borg',
        ownerEmail: contacts[0].email,
        effectiveFrom: '2026-09-10',
        effectiveTill: null,
        acquisitionPrice: 150000,
        acquisitionCurrency: 'EUR',
        acquisitionPriceUsd: 163100,
        isCurrent: true
      };

      await setup({ ownerships: [historical, current] });
      ownershipApi.createOwnership.mockReturnValue(of(newCurrent));
      ownershipApi.getOwnerships
        .mockReturnValueOnce(of([historical, current]))
        .mockReturnValueOnce(of([historical, closedPrevious, newCurrent]));

      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      openAndFillValidTransfer();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(ownershipApi.createOwnership).toHaveBeenCalledTimes(1);
      expect(ownershipApi.getOwnerships.mock.calls.length).toBeGreaterThanOrEqual(2);

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Joe Borg');
      expect(fixture.componentInstance.currentOwner()?.ownerLastName).toBe('Borg');
      expect(
        fixture.componentInstance.ownerships().find((item) => item.id === current.id)
          ?.effectiveTill
      ).toBe('2026-09-10');
    });
  });

  describe('errors', () => {
    beforeEach(async () => {
      await setup();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    });

    it('handles 404 property/contact errors', async () => {
      ownershipApi.createOwnership.mockReturnValue(
        throwError(
          () =>
            new ApiError(404, {
              title: 'Not Found',
              detail: "Contact 'missing' was not found."
            })
        )
      );

      openAndFillValidTransfer();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.banner-error')?.textContent).toContain(
        'not found'
      );
    });

    it('handles 409 overlap with a useful message', async () => {
      ownershipApi.createOwnership.mockReturnValue(
        throwError(
          () =>
            new ApiError(409, {
              title: 'Conflict',
              detail:
                'The ownership period overlaps an existing ownership period for this property.'
            })
        )
      );

      openAndFillValidTransfer();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.banner-error')?.textContent).toContain(
        'overlaps'
      );
    });
  });
});
