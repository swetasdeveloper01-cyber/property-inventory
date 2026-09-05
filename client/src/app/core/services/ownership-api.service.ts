import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { CreateOwnershipRequest, OwnershipDto } from '../models/ownership.models';

@Injectable({ providedIn: 'root' })
export class OwnershipApiService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly apiBaseUrl: string
  ) {}

  private ownershipsUrl(propertyId: string): string {
    return `${this.apiBaseUrl}/api/properties/${propertyId}/ownerships`;
  }

  getOwnerships(propertyId: string): Observable<OwnershipDto[]> {
    return this.http.get<OwnershipDto[]>(this.ownershipsUrl(propertyId));
  }

  createOwnership(propertyId: string, request: CreateOwnershipRequest): Observable<OwnershipDto> {
    return this.http.post<OwnershipDto>(this.ownershipsUrl(propertyId), request);
  }
}
