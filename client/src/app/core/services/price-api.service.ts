import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { CreatePropertyPriceRequest, PropertyPriceDto } from '../models/price.models';

@Injectable({ providedIn: 'root' })
export class PriceApiService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly apiBaseUrl: string
  ) {}

  private pricesUrl(propertyId: string): string {
    return `${this.apiBaseUrl}/api/properties/${propertyId}/prices`;
  }

  getPrices(propertyId: string): Observable<PropertyPriceDto[]> {
    return this.http.get<PropertyPriceDto[]>(this.pricesUrl(propertyId));
  }

  createPrice(propertyId: string, request: CreatePropertyPriceRequest): Observable<PropertyPriceDto> {
    return this.http.post<PropertyPriceDto>(this.pricesUrl(propertyId), request);
  }
}
