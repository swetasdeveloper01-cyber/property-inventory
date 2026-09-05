import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { toHttpParams } from '../http/http-params.util';
import {
  ContactDto,
  ContactQuery,
  CreateContactRequest,
  UpdateContactBatchItem,
  UpdateContactRequest
} from '../models/contact.models';
import { PagedResult } from '../models/paged-result';

@Injectable({ providedIn: 'root' })
export class ContactApiService {
  private readonly baseUrl: string;

  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) apiBaseUrl: string
  ) {
    this.baseUrl = `${apiBaseUrl}/api/contacts`;
  }

  getContacts(query: ContactQuery = {}): Observable<PagedResult<ContactDto>> {
    return this.http.get<PagedResult<ContactDto>>(this.baseUrl, {
      params: toHttpParams({ ...query })
    });
  }

  getContactById(id: string): Observable<ContactDto> {
    return this.http.get<ContactDto>(`${this.baseUrl}/${id}`);
  }

  createContact(request: CreateContactRequest): Observable<ContactDto> {
    return this.http.post<ContactDto>(this.baseUrl, request);
  }

  createContactsBatch(requests: CreateContactRequest[]): Observable<ContactDto[]> {
    return this.http.post<ContactDto[]>(`${this.baseUrl}/batch`, requests);
  }

  updateContact(id: string, request: UpdateContactRequest): Observable<ContactDto> {
    return this.http.put<ContactDto>(`${this.baseUrl}/${id}`, request);
  }

  updateContactsBatch(requests: UpdateContactBatchItem[]): Observable<ContactDto[]> {
    return this.http.put<ContactDto[]>(`${this.baseUrl}/batch`, requests);
  }
}
