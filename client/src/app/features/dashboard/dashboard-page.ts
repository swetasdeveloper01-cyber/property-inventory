import { Component, OnInit, inject, signal } from '@angular/core';
import { DashboardApiService } from '../../core/services/dashboard-api.service';
import { SalesDashboardItemDto } from '../../core/models/dashboard.models';
import { ApiError } from '../../core/models/problem-details';
import { formatBusinessDate, formatMoney, formatShortId } from '../../shared/utils/format';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.css'
})
export class DashboardPage implements OnInit {
  private readonly dashboardApi = inject(DashboardApiService);

  readonly sales = signal<SalesDashboardItemDto[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly formatMoney = formatMoney;
  readonly formatBusinessDate = formatBusinessDate;
  readonly formatShortId = formatShortId;

  ngOnInit(): void {
    this.loadSales();
  }

  loadSales(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.dashboardApi.getSales().subscribe({
      next: (items) => {
        this.sales.set(items);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.sales.set([]);
        this.loading.set(false);
        this.errorMessage.set(this.toUserMessage(error));
      }
    });
  }

  private toUserMessage(error: unknown): string {
    if (error instanceof ApiError && error.problem.detail) {
      return 'Unable to load sales data. Please try again.';
    }

    return 'Unable to load sales data. Please try again.';
  }
}
