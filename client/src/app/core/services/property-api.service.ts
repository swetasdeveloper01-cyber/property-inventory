import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { toHttpParams } from '../http/http-params.util';
import { PagedResult } from '../models/paged-result';
import {
  CreatePropertyRequest,
  PropertyDto,
  PropertyQuery,
  UpdatePropertyBatchItem,
  UpdatePropertyRequest
} from '../models/property.models';

@Injectable({ providedIn: 'root' })
export class PropertyApiService {
  private readonly baseUrl: string;

  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) apiBaseUrl: string
  ) {
    this.baseUrl = `${apiBaseUrl}/api/properties`;
  }

  getProperties(query: PropertyQuery = {}): Observable<PagedResult<PropertyDto>> {
    return this.http.get<PagedResult<PropertyDto>>(this.baseUrl, {
      params: toHttpParams({ ...query })
    });
  }

  getPropertyById(id: string): Observable<PropertyDto> {
    return this.http.get<PropertyDto>(`${this.baseUrl}/${id}`);
  }

  createProperty(request: CreatePropertyRequest): Observable<PropertyDto> {
    return this.http.post<PropertyDto>(this.baseUrl, request);
  }

  createPropertiesBatch(requests: CreatePropertyRequest[]): Observable<PropertyDto[]> {
    return this.http.post<PropertyDto[]>(`${this.baseUrl}/batch`, requests);
  }

  updateProperty(id: string, request: UpdatePropertyRequest): Observable<PropertyDto> {
    return this.http.put<PropertyDto>(`${this.baseUrl}/${id}`, request);
  }

  updatePropertiesBatch(requests: UpdatePropertyBatchItem[]): Observable<PropertyDto[]> {
    return this.http.put<PropertyDto[]>(`${this.baseUrl}/batch`, requests);
  }
}
