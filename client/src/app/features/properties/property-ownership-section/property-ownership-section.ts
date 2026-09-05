import { Component, Input, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { forkJoin, of, switchMap } from 'rxjs';
import { ContactDto } from '../../../core/models/contact.models';
import { CreateOwnershipRequest, OwnershipDto } from '../../../core/models/ownership.models';
import { ApiError } from '../../../core/models/problem-details';
import { ContactApiService } from '../../../core/services/contact-api.service';
import { OwnershipApiService } from '../../../core/services/ownership-api.service';
import { applyApiFieldErrors } from '../../../shared/utils/api-form-errors';
import { formatBusinessDate, formatMoney } from '../../../shared/utils/format';

@Component({
  selector: 'app-property-ownership-section',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './property-ownership-section.html',
  styleUrl: './property-ownership-section.css'
})
export class PropertyOwnershipSection implements OnChanges {
  private readonly ownershipApi = inject(OwnershipApiService);
  private readonly contactApi = inject(ContactApiService);
  private readonly fb = inject(FormBuilder);

  @Input({ required: true }) propertyId!: string;

  readonly ownerships = signal<OwnershipDto[]>([]);
  readonly contacts = signal<ContactDto[]>([]);
  readonly loading = signal(true);
  readonly contactsLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly contactsError = signal<string | null>(null);
  readonly formVisible = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = this.fb.group(
    {
      contactId: this.fb.nonNullable.control('', [Validators.required]),
      effectiveFrom: this.fb.nonNullable.control('', [Validators.required, isoDateValidator]),
      effectiveTill: this.fb.nonNullable.control('', [isoDateValidator]),
      acquisitionPrice: this.fb.control<number | null>(null, [
        Validators.required,
        Validators.min(0.01)
      ]),
      acquisitionCurrency: this.fb.nonNullable.control('EUR', [
        Validators.required,
        Validators.pattern(/^[A-Za-z]{3}$/)
      ])
    },
    { validators: [dateRangeValidator] }
  );

  readonly formatBusinessDate = formatBusinessDate;
  readonly formatMoney = formatMoney;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['propertyId'] && this.propertyId) {
      this.loadOwnerships();
    }
  }

  currentOwner(): OwnershipDto | null {
    return this.ownerships().find((item) => item.effectiveTill === null) ?? null;
  }

  isCurrent(ownership: OwnershipDto): boolean {
    return ownership.effectiveTill === null;
  }

  ownerName(ownership: OwnershipDto): string {
    return `${ownership.ownerFirstName} ${ownership.ownerLastName}`.trim();
  }

  dateRangeLabel(ownership: OwnershipDto): string {
    const from = formatBusinessDate(ownership.effectiveFrom);
    if (ownership.effectiveTill === null) {
      return `${from} → Present`;
    }

    return `${from} → ${formatBusinessDate(ownership.effectiveTill)}`;
  }

  actionLabel(): string {
    return this.currentOwner() ? 'Transfer Ownership' : 'Add Ownership';
  }

  openForm(): void {
    this.formVisible.set(true);
    this.formError.set(null);
    this.successMessage.set(null);
    this.form.reset({
      contactId: '',
      effectiveFrom: '',
      effectiveTill: '',
      acquisitionPrice: null,
      acquisitionCurrency: 'EUR'
    });
    this.loadContacts();
  }

  cancelForm(): void {
    this.formVisible.set(false);
    this.formError.set(null);
  }

  loadOwnerships(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.ownershipApi.getOwnerships(this.propertyId).subscribe({
      next: (items) => {
        this.ownerships.set(items);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.ownerships.set([]);
        this.loading.set(false);
        if (err instanceof ApiError && err.status === 404) {
          this.errorMessage.set('Property not found.');
          return;
        }

        this.errorMessage.set('Unable to load ownership history. Please try again.');
      }
    });
  }

  submit(): void {
    this.formError.set(null);
    this.successMessage.set(null);
    this.form.markAllAsTouched();

    if (this.form.invalid || this.saving()) {
      return;
    }

    const isTransfer = this.currentOwner() !== null;
    const payload = this.buildPayload();
    this.saving.set(true);

    this.ownershipApi.createOwnership(this.propertyId, payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.successMessage.set(
          isTransfer
            ? 'Ownership transferred successfully.'
            : 'Ownership recorded successfully.'
        );
        this.loadOwnerships();
      },
      error: (err: unknown) => this.handleSaveError(err)
    });
  }

  controlError(controlName: string): string | null {
    const control = this.form.get(controlName);
    if (!control || !control.touched || !control.errors) {
      return null;
    }

    if (control.errors['required']) {
      return 'This field is required.';
    }

    if (control.errors['min']) {
      return 'Acquisition price must be greater than zero.';
    }

    if (control.errors['pattern']) {
      return 'Currency must be a 3-letter ISO code.';
    }

    if (control.errors['isoDate']) {
      return 'Enter a valid date (yyyy-MM-dd).';
    }

    if (control.errors['api']) {
      return control.errors['api'] as string;
    }

    return 'Invalid value.';
  }

  dateRangeError(): string | null {
    if (!this.form.touched && !this.form.get('effectiveTill')?.touched) {
      return null;
    }

    if (this.form.errors?.['dateRange']) {
      return 'Effective Till must be after Effective From.';
    }

    return null;
  }

  private loadContacts(): void {
    this.contactsLoading.set(true);
    this.contactsError.set(null);

    this.contactApi
      .getContacts({ page: 1, pageSize: 100 })
      .pipe(
        switchMap((first) => {
          if (first.totalPages <= 1) {
            return of(first.items);
          }

          const requests = Array.from({ length: first.totalPages - 1 }, (_, index) =>
            this.contactApi.getContacts({ page: index + 2, pageSize: 100 })
          );

          return forkJoin(requests).pipe(
            switchMap((pages) => of([...first.items, ...pages.flatMap((page) => page.items)]))
          );
        })
      )
      .subscribe({
        next: (items) => {
          this.contacts.set(
            [...items].sort((a, b) =>
              `${a.lastName} ${a.firstName}`.localeCompare(`${b.lastName} ${b.firstName}`)
            )
          );
          this.contactsLoading.set(false);
        },
        error: () => {
          this.contacts.set([]);
          this.contactsLoading.set(false);
          this.contactsError.set('Unable to load contacts for owner selection.');
        }
      });
  }

  private buildPayload(): CreateOwnershipRequest {
    const raw = this.form.getRawValue();
    const till = String(raw.effectiveTill ?? '').trim();

    return {
      contactId: String(raw.contactId ?? '').trim(),
      effectiveFrom: String(raw.effectiveFrom ?? '').trim(),
      effectiveTill: till.length > 0 ? till : null,
      acquisitionPrice: Number(raw.acquisitionPrice),
      acquisitionCurrency: String(raw.acquisitionCurrency ?? '')
        .trim()
        .toUpperCase()
    };
  }

  private handleSaveError(err: unknown): void {
    this.saving.set(false);

    if (!(err instanceof ApiError)) {
      this.formError.set('Unable to save ownership. Please try again.');
      return;
    }

    if (err.status === 404) {
      this.formError.set(err.message || 'Property or contact was not found.');
      return;
    }

    if (err.status === 409) {
      this.formError.set(
        err.message || 'Ownership period overlaps an existing ownership record.'
      );
      return;
    }

    if (err.status === 400) {
      applyApiFieldErrors(err.fieldErrors, (controlName, message) => {
        const control = this.form.get(controlName);
        if (control) {
          control.setErrors({ ...(control.errors ?? {}), api: message });
          control.markAsTouched();
        }
      });

      this.formError.set(err.message || 'Please correct the highlighted fields.');
      return;
    }

    this.formError.set(err.message || 'Unable to save ownership. Please try again.');
  }
}

function isoDateValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '').trim();
  if (!value) {
    return null;
  }

  return /^\d{4}-\d{2}-\d{2}$/.test(value) ? null : { isoDate: true };
}

function dateRangeValidator(group: AbstractControl): ValidationErrors | null {
  const from = String(group.get('effectiveFrom')?.value ?? '').trim();
  const till = String(group.get('effectiveTill')?.value ?? '').trim();

  if (!from || !till) {
    return null;
  }

  return till > from ? null : { dateRange: true };
}
