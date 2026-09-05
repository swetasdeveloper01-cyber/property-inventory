import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';
import { SalesDashboardItemDto } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly baseUrl: string;

  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) apiBaseUrl: string
  ) {
    this.baseUrl = `${apiBaseUrl}/api/dashboard`;
  }

  getSales(): Observable<SalesDashboardItemDto[]> {
    return this.http.get<SalesDashboardItemDto[]>(`${this.baseUrl}/sales`);
  }
}
