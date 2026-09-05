import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../core/models/problem-details';
import { PropertyPriceDto } from '../../../core/models/price.models';
import { PriceApiService } from '../../../core/services/price-api.service';
import { PropertyApiService } from '../../../core/services/property-api.service';
import { PropertyPriceHistorySection } from './property-price-history-section';

describe('PropertyPriceHistorySection', () => {
  let fixture: ComponentFixture<PropertyPriceHistorySection>;
  let priceApi: {
    getPrices: ReturnType<typeof vi.fn>;
    createPrice: ReturnType<typeof vi.fn>;
  };
  let propertyApi: {
    updateProperty: ReturnType<typeof vi.fn>;
    getPropertyById: ReturnType<typeof vi.fn>;
  };

  const propertyId = 'a1111111-1111-1111-1111-111111111111';

  const history: PropertyPriceDto[] = [
    {
      id: 'p1111111-1111-1111-1111-111111111111',
      propertyId,
      amount: 130000,
      currency: 'EUR',
      effectiveDate: '2024-01-01'
    },
    {
      id: 'p2222222-2222-2222-2222-222222222222',
      propertyId,
      amount: 135000,
      currency: 'EUR',
      effectiveDate: '2024-06-15'
    },
    {
      id: 'p3333333-3333-3333-3333-333333333333',
      propertyId,
      amount: 140000,
      currency: 'EUR',
      effectiveDate: '2024-09-01'
    }
  ];

  async function setup(options?: {
    prices?: PropertyPriceDto[];
    loadError?: unknown;
  }): Promise<void> {
    TestBed.resetTestingModule();

    priceApi = {
      getPrices: vi.fn(() =>
        options?.loadError
          ? throwError(() => options.loadError)
          : of(options?.prices ?? history)
      ),
      createPrice: vi.fn(() =>
        of({
          id: 'p-new',
          propertyId,
          amount: 145000,
          currency: 'EUR',
          effectiveDate: '2026-09-05'
        })
      )
    };

    propertyApi = {
      updateProperty: vi.fn(() => of({})),
      getPropertyById: vi.fn(() => of({}))
    };

    await TestBed.configureTestingModule({
      imports: [PropertyPriceHistorySection],
      providers: [
        { provide: PriceApiService, useValue: priceApi },
        { provide: PropertyApiService, useValue: propertyApi }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyPriceHistorySection);
    fixture.componentRef.setInput('propertyId', propertyId);
    fixture.componentRef.setInput('currentPrice', 140000);
    fixture.componentRef.setInput('currentCurrency', 'EUR');
  }

  function openAndFillValid(): void {
    fixture.componentInstance.openForm();
    fixture.detectChanges();
    fixture.componentInstance.form.setValue({
      amount: 145000,
      currency: 'EUR',
      effectiveDate: '2026-09-05'
    });
  }

  describe('history', () => {
    beforeEach(async () => {
      await setup();
    });

    it('loads price history', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      expect(priceApi.getPrices).toHaveBeenCalledWith(propertyId);
    });

    it('renders price history records with amount, currency, and dates', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('130,000.00');
      expect(text).toContain('135,000.00');
      expect(text).toContain('140,000.00');
      expect(text).toContain('1 Jan 2024');
      expect(text).toContain('15 Jun 2024');
      expect(text).toContain('1 Sep 2024');
    });

    it('displays records in backend order', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      const cells = Array.from(
        fixture.nativeElement.querySelectorAll('.data-table tbody tr td:first-child')
      ).map((cell) => (cell as HTMLElement).textContent?.trim());

      expect(cells).toEqual(['1 Jan 2024', '15 Jun 2024', '1 Sep 2024']);
    });

    it('shows current asking price distinctly from history rows', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.current-summary')?.textContent).toContain(
        'Current asking price'
      );
      expect(fixture.nativeElement.querySelector('.current-summary')?.textContent).toContain(
        '140,000.00'
      );
      expect(fixture.nativeElement.querySelectorAll('.data-table tbody tr').length).toBe(3);
    });

    it('shows loading state', () => {
      priceApi.getPrices.mockReturnValue({
        subscribe: () => ({ unsubscribe() {} })
      });

      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-loading')?.textContent).toContain(
        'Loading asking price history'
      );
      expect(fixture.nativeElement.querySelector('.data-table')).toBeNull();
    });

    it('shows empty state', async () => {
      await setup({ prices: [] });
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-empty')?.textContent).toContain(
        'No asking price history recorded.'
      );
    });

    it('shows error state and retries', async () => {
      await setup({
        loadError: new ApiError(500, { title: 'Server Error', detail: 'boom' })
      });
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-error')?.textContent).toContain(
        'Unable to load asking price history'
      );

      priceApi.getPrices.mockReturnValue(of(history));
      const retry = fixture.nativeElement.querySelector('.retry-button') as HTMLButtonElement;
      retry.click();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(priceApi.getPrices).toHaveBeenCalledTimes(2);
      expect(fixture.nativeElement.textContent).toContain('130,000.00');
    });
  });

  describe('form', () => {
    beforeEach(async () => {
      await setup();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    });

    it('opens the Record Price Change form', () => {
      fixture.componentInstance.openForm();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.price-form')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('h3')?.textContent).toContain(
        'Record Price Change'
      );
      expect(fixture.nativeElement.querySelector('#price-amount')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('#price-currency')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('#price-effective-date')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('#ownership-price')).toBeNull();
      expect(Object.keys(fixture.componentInstance.form.controls)).toEqual([
        'amount',
        'currency',
        'effectiveDate'
      ]);
    });

    it('requires amount', () => {
      openAndFillValid();
      fixture.componentInstance.form.patchValue({ amount: null });
      fixture.componentInstance.submit();

      expect(priceApi.createPrice).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('amount')).toContain('required');
    });

    it('requires amount greater than zero', () => {
      openAndFillValid();
      fixture.componentInstance.form.patchValue({ amount: 0 });
      fixture.componentInstance.submit();

      expect(priceApi.createPrice).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('amount')).toContain('greater than zero');
    });

    it('requires currency', () => {
      openAndFillValid();
      fixture.componentInstance.form.patchValue({ currency: '' });
      fixture.componentInstance.submit();

      expect(priceApi.createPrice).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('currency')).toContain('required');
    });

    it('validates currency format', () => {
      openAndFillValid();
      fixture.componentInstance.form.patchValue({ currency: 'EU' });
      fixture.componentInstance.submit();

      expect(priceApi.createPrice).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('currency')).toContain('3-letter');
    });

    it('requires Effective Date', () => {
      openAndFillValid();
      fixture.componentInstance.form.patchValue({ effectiveDate: '' });
      fixture.componentInstance.submit();

      expect(priceApi.createPrice).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('effectiveDate')).toContain('required');
    });

    it('does not call API for an invalid form', () => {
      fixture.componentInstance.openForm();
      fixture.detectChanges();
      fixture.componentInstance.submit();

      expect(priceApi.createPrice).not.toHaveBeenCalled();
    });

    it('valid form calls POST price API only with expected payload', () => {
      openAndFillValid();
      fixture.componentInstance.submit();

      expect(priceApi.createPrice).toHaveBeenCalledTimes(1);
      expect(priceApi.createPrice).toHaveBeenCalledWith(propertyId, {
        amount: 145000,
        currency: 'EUR',
        effectiveDate: '2026-09-05'
      });
      expect(propertyApi.updateProperty).not.toHaveBeenCalled();
      expect(propertyApi.getPropertyById).not.toHaveBeenCalled();

      const payload = priceApi.createPrice.mock.calls[0][1];
      expect(payload).not.toHaveProperty('acquisitionPrice');
      expect(payload).not.toHaveProperty('acquisitionPriceUsd');
    });

    it('disables save while saving', () => {
      priceApi.createPrice.mockReturnValue({
        subscribe: () => ({ unsubscribe() {} })
      });

      openAndFillValid();
      fixture.componentInstance.submit();
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector(
        'button[type="submit"]'
      ) as HTMLButtonElement;
      expect(button.disabled).toBe(true);
      expect(button.textContent).toContain('Saving');
    });

    it('displays API validation errors', async () => {
      priceApi.createPrice.mockReturnValue(
        throwError(
          () =>
            new ApiError(400, {
              title: 'Validation failed',
              detail: 'One or more validation errors occurred.',
              errors: { Amount: ['Amount must be a positive decimal value.'] }
            })
        )
      );

      openAndFillValid();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.banner-error')?.textContent).toContain(
        'validation'
      );
      expect(fixture.componentInstance.controlError('amount')).toBe(
        'Amount must be a positive decimal value.'
      );
    });

    it('successful save reloads history and emits asking price change', async () => {
      const created = {
        id: 'p-new',
        propertyId,
        amount: 145000,
        currency: 'EUR',
        effectiveDate: '2026-09-05'
      };

      priceApi.createPrice.mockReturnValue(of(created));
      priceApi.getPrices
        .mockReturnValueOnce(of(history))
        .mockReturnValueOnce(of([...history, created]));

      fixture.componentInstance.loadPrices();
      await fixture.whenStable();
      fixture.detectChanges();

      const emitSpy = vi.fn();
      fixture.componentInstance.askingPriceChanged.subscribe(emitSpy);

      openAndFillValid();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(emitSpy).toHaveBeenCalledWith({ price: 145000, currency: 'EUR' });
      expect(priceApi.getPrices.mock.calls.length).toBeGreaterThanOrEqual(2);
      expect(fixture.nativeElement.textContent).toContain('145,000.00');
      expect(propertyApi.updateProperty).not.toHaveBeenCalled();
    });
  });

  describe('errors', () => {
    beforeEach(async () => {
      await setup();
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    });

    it('handles property not found on load', async () => {
      await setup({
        loadError: new ApiError(404, { title: 'Not Found', detail: 'Property not found.' })
      });
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-error')?.textContent).toContain(
        'Property not found'
      );
    });

    it('handles price-history API errors on save', async () => {
      priceApi.createPrice.mockReturnValue(
        throwError(
          () =>
            new ApiError(404, {
              title: 'Not Found',
              detail: "Property 'missing' was not found."
            })
        )
      );

      openAndFillValid();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.banner-error')?.textContent).toContain(
        'not found'
      );
    });
  });
});
