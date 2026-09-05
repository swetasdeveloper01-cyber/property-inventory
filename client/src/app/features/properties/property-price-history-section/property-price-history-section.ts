import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import {
  CreatePropertyPriceRequest,
  PropertyPriceDto
} from '../../../core/models/price.models';
import { ApiError } from '../../../core/models/problem-details';
import { PriceApiService } from '../../../core/services/price-api.service';
import { applyApiFieldErrors } from '../../../shared/utils/api-form-errors';
import { formatBusinessDate, formatMoney } from '../../../shared/utils/format';

@Component({
  selector: 'app-property-price-history-section',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './property-price-history-section.html',
  styleUrl: './property-price-history-section.css'
})
export class PropertyPriceHistorySection implements OnChanges {
  private readonly priceApi = inject(PriceApiService);
  private readonly fb = inject(FormBuilder);

  @Input({ required: true }) propertyId!: string;
  @Input() currentPrice: number | null = null;
  @Input() currentCurrency: string | null = null;
  @Output() readonly askingPriceChanged = new EventEmitter<{ price: number; currency: string }>();

  readonly prices = signal<PropertyPriceDto[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly formVisible = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    amount: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.01)]),
    currency: this.fb.nonNullable.control('EUR', [
      Validators.required,
      Validators.pattern(/^[A-Za-z]{3}$/)
    ]),
    effectiveDate: this.fb.nonNullable.control('', [Validators.required, isoDateValidator])
  });

  readonly formatBusinessDate = formatBusinessDate;
  readonly formatMoney = formatMoney;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['propertyId'] && this.propertyId) {
      this.loadPrices();
    }
  }

  openForm(): void {
    this.formVisible.set(true);
    this.formError.set(null);
    this.successMessage.set(null);
    this.form.reset({
      amount: null,
      currency: (this.currentCurrency ?? 'EUR').toUpperCase(),
      effectiveDate: ''
    });
  }

  cancelForm(): void {
    this.formVisible.set(false);
    this.formError.set(null);
  }

  loadPrices(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.priceApi.getPrices(this.propertyId).subscribe({
      next: (items) => {
        this.prices.set(items);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.prices.set([]);
        this.loading.set(false);
        if (err instanceof ApiError && err.status === 404) {
          this.errorMessage.set('Property not found.');
          return;
        }

        this.errorMessage.set('Unable to load asking price history. Please try again.');
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

    const payload = this.buildPayload();
    this.saving.set(true);

    this.priceApi.createPrice(this.propertyId, payload).subscribe({
      next: (created) => {
        this.saving.set(false);
        this.formVisible.set(false);
        this.successMessage.set('Asking price change recorded successfully.');
        this.askingPriceChanged.emit({
          price: created.amount,
          currency: created.currency
        });
        this.loadPrices();
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
      return 'Amount must be greater than zero.';
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

  private buildPayload(): CreatePropertyPriceRequest {
    const raw = this.form.getRawValue();
    return {
      amount: Number(raw.amount),
      currency: String(raw.currency ?? '')
        .trim()
        .toUpperCase(),
      effectiveDate: String(raw.effectiveDate ?? '').trim()
    };
  }

  private handleSaveError(err: unknown): void {
    this.saving.set(false);

    if (!(err instanceof ApiError)) {
      this.formError.set('Unable to record the price change. Please try again.');
      return;
    }

    if (err.status === 404) {
      this.formError.set(err.message || 'Property not found.');
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

    this.formError.set(err.message || 'Unable to record the price change. Please try again.');
  }
}

function isoDateValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '').trim();
  if (!value) {
    return null;
  }

  return /^\d{4}-\d{2}-\d{2}$/.test(value) ? null : { isoDate: true };
}
