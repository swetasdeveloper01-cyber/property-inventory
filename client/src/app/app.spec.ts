import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from './core/config/api-config';
import { problemDetailsInterceptor } from './core/interceptors/problem-details.interceptor';
import { ApiError } from './core/models/problem-details';
import { ContactApiService } from './core/services/contact-api.service';
import { DashboardApiService } from './core/services/dashboard-api.service';
import { OwnershipApiService } from './core/services/ownership-api.service';
import { PriceApiService } from './core/services/price-api.service';
import { PropertyApiService } from './core/services/property-api.service';
import { routes } from './app.routes';

describe('Angular API foundation', () => {
  const apiBaseUrl = 'http://localhost:5248';
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: apiBaseUrl }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('PropertyApiService builds list URL with query params', () => {
    const service = TestBed.inject(PropertyApiService);

    service.getProperties({ page: 2, pageSize: 5, name: 'Maison' }).subscribe();

    const req = httpMock.expectOne(
      `${apiBaseUrl}/api/properties?page=2&pageSize=5&name=Maison`
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], page: 2, pageSize: 5, totalCount: 0, totalPages: 0 });
  });

  it('PropertyApiService posts create and batch endpoints', () => {
    const service = TestBed.inject(PropertyApiService);
    const body = {
      name: 'Flat',
      address: 'Addr',
      price: 100000,
      currency: 'EUR',
      dateOfRegistration: '2025-01-01'
    };

    service.createProperty(body).subscribe();
    const createReq = httpMock.expectOne(`${apiBaseUrl}/api/properties`);
    expect(createReq.request.method).toBe('POST');
    createReq.flush({ id: 'p1', ...body });

    service.createPropertiesBatch([body]).subscribe();
    const batchReq = httpMock.expectOne(`${apiBaseUrl}/api/properties/batch`);
    expect(batchReq.request.method).toBe('POST');
    batchReq.flush([{ id: 'p1', ...body }]);
  });

  it('ContactApiService builds contact URLs', () => {
    const service = TestBed.inject(ContactApiService);

    service.getContactById('c1').subscribe();
    const getReq = httpMock.expectOne(`${apiBaseUrl}/api/contacts/c1`);
    expect(getReq.request.method).toBe('GET');
    getReq.flush({
      id: 'c1',
      firstName: 'A',
      lastName: 'B',
      phoneNumber: '1',
      email: 'a@example.com'
    });
  });

  it('DashboardApiService calls sales endpoint', () => {
    const service = TestBed.inject(DashboardApiService);

    service.getSales().subscribe((items) => {
      expect(items.length).toBe(1);
      expect(items[0].owner).toBe('Carmen Attard');
    });

    const req = httpMock.expectOne(`${apiBaseUrl}/api/dashboard/sales`);
    expect(req.request.method).toBe('GET');
    req.flush([
      {
        id: 'o1',
        propertyName: 'Maisonette',
        askingPrice: 130000,
        askingCurrency: 'EUR',
        owner: 'Carmen Attard',
        dateOfPurchase: '2024-01-15',
        soldAtPrice: 120000,
        soldAtCurrency: 'EUR',
        soldAtPriceUsd: 130480
      }
    ]);
  });

  it('Ownership and Price services use nested property routes', () => {
    const ownerships = TestBed.inject(OwnershipApiService);
    const prices = TestBed.inject(PriceApiService);

    ownerships.getOwnerships('p1').subscribe();
    httpMock.expectOne(`${apiBaseUrl}/api/properties/p1/ownerships`).flush([]);

    prices.createPrice('p1', {
      amount: 120000,
      currency: 'EUR',
      effectiveDate: '2026-01-01'
    }).subscribe();
    const priceReq = httpMock.expectOne(`${apiBaseUrl}/api/properties/p1/prices`);
    expect(priceReq.request.method).toBe('POST');
    priceReq.flush({
      id: 'ph1',
      propertyId: 'p1',
      amount: 120000,
      currency: 'EUR',
      effectiveDate: '2026-01-01'
    });
  });

  it('problemDetailsInterceptor maps validation ProblemDetails to ApiError', () => {
    const service = TestBed.inject(PropertyApiService);

    service.getPropertyById('missing').subscribe({
      next: () => {
        throw new Error('expected error');
      },
      error: (error: unknown) => {
        expect(error).toBeInstanceOf(ApiError);
        const apiError = error as ApiError;
        expect(apiError.status).toBe(400);
        expect(apiError.fieldErrors['Name']).toEqual(['Name is required.']);
      }
    });

    const req = httpMock.expectOne(`${apiBaseUrl}/api/properties/missing`);
    req.flush(
      {
        title: 'Validation Error',
        status: 400,
        detail: 'One or more validation errors occurred.',
        errors: { Name: ['Name is required.'] }
      },
      { status: 400, statusText: 'Bad Request' }
    );
  });

  it('routes include dashboard, properties, and contacts', () => {
    const paths = routes.map((route) => route.path);
    expect(paths).toContain('dashboard');
    expect(paths).toContain('properties');
    expect(paths).toContain('contacts');
  });
});
