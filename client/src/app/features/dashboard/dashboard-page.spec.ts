import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../core/models/problem-details';
import { SalesDashboardItemDto } from '../../core/models/dashboard.models';
import { DashboardApiService } from '../../core/services/dashboard-api.service';
import { DashboardPage } from './dashboard-page';

describe('DashboardPage', () => {
  let fixture: ComponentFixture<DashboardPage>;
  let api: { getSales: ReturnType<typeof vi.fn> };

  const sample: SalesDashboardItemDto[] = [
    {
      id: 'e2222222-2222-2222-2222-222222222222',
      propertyName: 'Maisonette',
      askingPrice: 130000,
      askingCurrency: 'EUR',
      owner: 'Carmen Attard',
      dateOfPurchase: '2024-01-15',
      soldAtPrice: 120000,
      soldAtCurrency: 'EUR',
      soldAtPriceUsd: 130480
    }
  ];

  beforeEach(async () => {
    api = {
      getSales: vi.fn(() => of(sample))
    };

    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [{ provide: DashboardApiService, useValue: api }]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPage);
  });

  it('renders successfully and calls DashboardApiService', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.getSales).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.querySelector('h1')?.textContent).toContain('Sales Dashboard');
  });

  it('renders sales fields from the API response', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Maisonette');
    expect(text).toContain('Carmen Attard');
    expect(text).toContain('15 Jan 2024');
    expect(text).toContain('130,000.00');
    expect(text).toContain('120,000.00');
    expect(text).toContain('130,480.00');
    expect(text).toContain('e2222222…');
  });

  it('shows loading state while the request is pending', () => {
    api.getSales.mockReturnValue({
      subscribe: () => ({ unsubscribe() {} })
    });

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-loading')?.textContent).toContain(
      'Loading sales data'
    );
    expect(fixture.nativeElement.querySelector('.sales-table')).toBeNull();
  });

  it('shows empty state when API returns no rows', async () => {
    api.getSales.mockReturnValue(of([]));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-empty')?.textContent).toContain(
      'No ownership acquisition events recorded yet.'
    );
  });

  it('shows error state and retries the API call', async () => {
    api.getSales
      .mockReturnValueOnce(
        throwError(
          () =>
            new ApiError(500, {
              title: 'Server Error',
              detail: 'boom'
            })
        )
      )
      .mockReturnValueOnce(of(sample));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.state-error')?.textContent).toContain(
      'Unable to load sales data'
    );

    const retry = fixture.nativeElement.querySelector('.retry-button') as HTMLButtonElement;
    retry.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.getSales).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.textContent).toContain('Maisonette');
  });
});
