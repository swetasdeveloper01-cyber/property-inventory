import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../core/models/problem-details';
import { PropertyDto } from '../../../core/models/property.models';
import { ContactApiService } from '../../../core/services/contact-api.service';
import { OwnershipApiService } from '../../../core/services/ownership-api.service';
import { PriceApiService } from '../../../core/services/price-api.service';
import { PropertyApiService } from '../../../core/services/property-api.service';
import { PropertyFormPage } from './property-form-page';

describe('PropertyFormPage', () => {
  let fixture: ComponentFixture<PropertyFormPage>;
  let api: {
    getPropertyById: ReturnType<typeof vi.fn>;
    createProperty: ReturnType<typeof vi.fn>;
    updateProperty: ReturnType<typeof vi.fn>;
  };
  let priceApi: {
    getPrices: ReturnType<typeof vi.fn>;
    createPrice: ReturnType<typeof vi.fn>;
  };
  let ownershipApi: {
    getOwnerships: ReturnType<typeof vi.fn>;
    createOwnership: ReturnType<typeof vi.fn>;
  };
  let contactApi: {
    getContacts: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  const existing: PropertyDto = {
    id: 'a1111111-1111-1111-1111-111111111111',
    name: 'Maisonette',
    address: '12 High Street',
    price: 130000,
    currency: 'EUR',
    dateOfRegistration: '2020-03-15'
  };

  async function setup(mode: 'create' | 'edit', options?: { loadError?: unknown }) {
    TestBed.resetTestingModule();

    api = {
      getPropertyById: vi.fn(() =>
        options?.loadError ? throwError(() => options.loadError) : of(existing)
      ),
      createProperty: vi.fn(() => of({ ...existing, id: 'new-id' })),
      updateProperty: vi.fn(() => of(existing))
    };

    priceApi = {
      getPrices: vi.fn(() => of([])),
      createPrice: vi.fn(() => of({}))
    };

    ownershipApi = {
      getOwnerships: vi.fn(() => of([])),
      createOwnership: vi.fn(() => of({}))
    };

    contactApi = {
      getContacts: vi.fn(() =>
        of({ items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 })
      )
    };

    const paramMap =
      mode === 'create'
        ? convertToParamMap({})
        : convertToParamMap({ id: existing.id });

    await TestBed.configureTestingModule({
      imports: [PropertyFormPage],
      providers: [
        { provide: PropertyApiService, useValue: api },
        { provide: PriceApiService, useValue: priceApi },
        { provide: OwnershipApiService, useValue: ownershipApi },
        { provide: ContactApiService, useValue: contactApi },
        provideRouter([{ path: 'properties', component: PropertyFormPage }]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyFormPage);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  }

  function fillValidForm(): void {
    const component = fixture.componentInstance;
    component.form.setValue({
      name: 'Townhouse',
      address: '99 Harbour Road',
      price: 250000,
      currency: 'EUR',
      dateOfRegistration: '2021-06-01'
    });
  }

  describe('create', () => {
    beforeEach(async () => {
      await setup('create');
    });

    it('renders the create form', () => {
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('h1')?.textContent).toContain('Add Property');
      expect(fixture.nativeElement.querySelector('#property-name')).toBeTruthy();
    });

    it('required validation works', () => {
      fixture.detectChanges();
      const component = fixture.componentInstance;
      component.form.reset({
        name: '',
        address: '',
        price: null,
        currency: '',
        dateOfRegistration: ''
      });
      component.submit();
      fixture.detectChanges();

      expect(api.createProperty).not.toHaveBeenCalled();
      expect(component.controlError('name')).toContain('required');
      expect(component.controlError('address')).toContain('required');
    });

    it('invalid price cannot be submitted', () => {
      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.form.patchValue({ price: -10 });
      fixture.componentInstance.submit();

      expect(api.createProperty).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('price')).toContain('negative');
    });

    it('invalid form does not call API', () => {
      fixture.detectChanges();
      fixture.componentInstance.submit();
      expect(api.createProperty).not.toHaveBeenCalled();
    });

    it('valid form calls create API with expected payload', () => {
      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.submit();

      expect(api.createProperty).toHaveBeenCalledWith({
        name: 'Townhouse',
        address: '99 Harbour Road',
        price: 250000,
        currency: 'EUR',
        dateOfRegistration: '2021-06-01'
      });
      expect(priceApi.createPrice).not.toHaveBeenCalled();
      expect(priceApi.getPrices).not.toHaveBeenCalled();
    });

    it('successful creation navigates to the list', async () => {
      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.submit();
      await fixture.whenStable();

      expect(router.navigate).toHaveBeenCalledWith(['/properties'], {
        state: { message: 'Property created successfully.' }
      });
    });

    it('API validation error is displayed', async () => {
      api.createProperty.mockReturnValue(
        throwError(
          () =>
            new ApiError(400, {
              title: 'Validation failed',
              detail: 'One or more validation errors occurred.',
              errors: { Name: ['Name is required.'] }
            })
        )
      );

      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.banner-error')?.textContent).toContain(
        'validation'
      );
      expect(fixture.componentInstance.controlError('name')).toBe('Name is required.');
    });

    it('property price editing uses Price/Currency and does not submit ownership fields', () => {
      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.submit();

      const payload = api.createProperty.mock.calls[0][0];
      expect(payload).toEqual({
        name: 'Townhouse',
        address: '99 Harbour Road',
        price: 250000,
        currency: 'EUR',
        dateOfRegistration: '2021-06-01'
      });
      expect(payload).not.toHaveProperty('acquisitionPrice');
      expect(payload).not.toHaveProperty('acquisitionCurrency');
      expect(payload).not.toHaveProperty('soldAtPrice');
    });
  });

  describe('edit', () => {
    beforeEach(async () => {
      await setup('edit');
    });

    it('loads the property by ID', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      expect(api.getPropertyById).toHaveBeenCalledWith(existing.id);
    });

    it('existing values populate the form', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.componentInstance.form.getRawValue()).toEqual({
        name: 'Maisonette',
        address: '12 High Street',
        price: 130000,
        currency: 'EUR',
        dateOfRegistration: '2020-03-15'
      });
    });

    it('update sends the expected payload and does not call price-history POST', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ price: 140000, currency: 'usd' });
      fixture.componentInstance.submit();

      expect(api.updateProperty).toHaveBeenCalledWith(existing.id, {
        name: 'Maisonette',
        address: '12 High Street',
        price: 140000,
        currency: 'USD',
        dateOfRegistration: '2020-03-15'
      });
      expect(priceApi.createPrice).not.toHaveBeenCalled();
    });

    it('syncs current asking price after price-history section emits a change', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      fixture.componentInstance.onAskingPriceChanged({ price: 145000, currency: 'USD' });

      expect(fixture.componentInstance.form.getRawValue().price).toBe(145000);
      expect(fixture.componentInstance.form.getRawValue().currency).toBe('USD');
    });

    it('missing property / API 404 displays an error', async () => {
      await setup('edit', {
        loadError: new ApiError(404, { title: 'Not Found', detail: 'Property not found.' })
      });

      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-error')?.textContent).toContain(
        'Property not found'
      );
      expect(fixture.nativeElement.querySelector('.property-form')).toBeNull();
    });

    it('successful update provides navigation feedback', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      fixture.componentInstance.submit();
      await fixture.whenStable();

      expect(router.navigate).toHaveBeenCalledWith(['/properties'], {
        state: { message: 'Property updated successfully.' }
      });
    });

    it('save button is disabled while saving', async () => {
      api.updateProperty.mockReturnValue({
        subscribe: () => ({ unsubscribe() {} })
      });

      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      fixture.componentInstance.submit();
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector(
        'button[type="submit"]'
      ) as HTMLButtonElement;
      expect(button.disabled).toBe(true);
      expect(button.textContent).toContain('Saving');
    });
  });
});
