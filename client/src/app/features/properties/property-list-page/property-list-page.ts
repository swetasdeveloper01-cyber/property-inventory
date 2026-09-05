import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PropertyApiService } from '../../../core/services/property-api.service';
import { PropertyDto, PropertyQuery } from '../../../core/models/property.models';
import { PagedResult } from '../../../core/models/paged-result';
import { formatBusinessDate, formatMoney, formatShortId } from '../../../shared/utils/format';

@Component({
  selector: 'app-property-list-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './property-list-page.html',
  styleUrl: './property-list-page.css'
})
export class PropertyListPage implements OnInit {
  private readonly propertyApi = inject(PropertyApiService);
  private readonly fb = inject(FormBuilder);

  readonly properties = signal<PropertyDto[]>([]);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly filterForm = this.fb.nonNullable.group({
    name: [''],
    address: [''],
    minPrice: [''],
    maxPrice: ['']
  });

  private appliedFilters: PropertyQuery = {};

  readonly formatMoney = formatMoney;
  readonly formatBusinessDate = formatBusinessDate;
  readonly formatShortId = formatShortId;

  ngOnInit(): void {
    const stateMessage = history.state?.['message'] as string | undefined;
    if (stateMessage) {
      this.successMessage.set(stateMessage);
      history.replaceState({}, '');
    }

    this.loadProperties();
  }

  applyFilters(): void {
    const raw = this.filterForm.getRawValue();
    this.appliedFilters = {
      name: raw.name.trim() || undefined,
      address: raw.address.trim() || undefined,
      minPrice: this.parseOptionalNumber(raw.minPrice),
      maxPrice: this.parseOptionalNumber(raw.maxPrice)
    };
    this.page.set(1);
    this.loadProperties();
  }

  clearFilters(): void {
    this.filterForm.reset({
      name: '',
      address: '',
      minPrice: '',
      maxPrice: ''
    });
    this.appliedFilters = {};
    this.page.set(1);
    this.loadProperties();
  }

  goToPage(nextPage: number): void {
    if (nextPage < 1 || nextPage > this.totalPages() || nextPage === this.page()) {
      return;
    }

    this.page.set(nextPage);
    this.loadProperties();
  }

  loadProperties(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const query: PropertyQuery = {
      ...this.appliedFilters,
      page: this.page(),
      pageSize: this.pageSize()
    };

    this.propertyApi.getProperties(query).subscribe({
      next: (result: PagedResult<PropertyDto>) => {
        this.properties.set(result.items);
        this.page.set(result.page);
        this.pageSize.set(result.pageSize);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => {
        this.properties.set([]);
        this.loading.set(false);
        this.errorMessage.set('Unable to load properties. Please try again.');
      }
    });
  }

  dismissSuccess(): void {
    this.successMessage.set(null);
  }

  private parseOptionalNumber(value: string): number | undefined {
    const trimmed = value.trim();
    if (!trimmed) {
      return undefined;
    }

    const parsed = Number(trimmed);
    return Number.isFinite(parsed) ? parsed : undefined;
  }
}
