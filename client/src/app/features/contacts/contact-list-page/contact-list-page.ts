import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ContactDto, ContactQuery } from '../../../core/models/contact.models';
import { PagedResult } from '../../../core/models/paged-result';
import { ContactApiService } from '../../../core/services/contact-api.service';
import { formatShortId } from '../../../shared/utils/format';

@Component({
  selector: 'app-contact-list-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './contact-list-page.html',
  styleUrl: './contact-list-page.css'
})
export class ContactListPage implements OnInit {
  private readonly contactApi = inject(ContactApiService);
  private readonly fb = inject(FormBuilder);

  readonly contacts = signal<ContactDto[]>([]);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly filterForm = this.fb.nonNullable.group({
    firstName: [''],
    lastName: [''],
    email: [''],
    phone: ['']
  });

  private appliedFilters: ContactQuery = {};

  readonly formatShortId = formatShortId;

  ngOnInit(): void {
    const stateMessage = history.state?.['message'] as string | undefined;
    if (stateMessage) {
      this.successMessage.set(stateMessage);
      history.replaceState({}, '');
    }

    this.loadContacts();
  }

  applyFilters(): void {
    const raw = this.filterForm.getRawValue();
    this.appliedFilters = {
      firstName: raw.firstName.trim() || undefined,
      lastName: raw.lastName.trim() || undefined,
      email: raw.email.trim() || undefined,
      phone: raw.phone.trim() || undefined
    };
    this.page.set(1);
    this.loadContacts();
  }

  clearFilters(): void {
    this.filterForm.reset({
      firstName: '',
      lastName: '',
      email: '',
      phone: ''
    });
    this.appliedFilters = {};
    this.page.set(1);
    this.loadContacts();
  }

  goToPage(nextPage: number): void {
    if (nextPage < 1 || nextPage > this.totalPages() || nextPage === this.page()) {
      return;
    }

    this.page.set(nextPage);
    this.loadContacts();
  }

  loadContacts(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const query: ContactQuery = {
      ...this.appliedFilters,
      page: this.page(),
      pageSize: this.pageSize()
    };

    this.contactApi.getContacts(query).subscribe({
      next: (result: PagedResult<ContactDto>) => {
        this.contacts.set(result.items);
        this.page.set(result.page);
        this.pageSize.set(result.pageSize);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => {
        this.contacts.set([]);
        this.loading.set(false);
        this.errorMessage.set('Unable to load contacts. Please try again.');
      }
    });
  }

  dismissSuccess(): void {
    this.successMessage.set(null);
  }
}
