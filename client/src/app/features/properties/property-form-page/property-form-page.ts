import { Component, OnInit, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  CreatePropertyRequest,
  PropertyDto,
  UpdatePropertyRequest
} from '../../../core/models/property.models';
import { ApiError } from '../../../core/models/problem-details';
import { PropertyApiService } from '../../../core/services/property-api.service';
import { applyApiFieldErrors } from '../../../shared/utils/api-form-errors';
import { formatShortId } from '../../../shared/utils/format';

@Component({
  selector: 'app-property-form-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './property-form-page.html',
  styleUrl: './property-form-page.css'
})
export class PropertyFormPage implements OnInit {
  private readonly propertyApi = inject(PropertyApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly isCreate = signal(true);
  readonly propertyId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = this.fb.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    address: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(500)]),
    price: this.fb.control<number | null>(0, [Validators.required, Validators.min(0)]),
    currency: this.fb.nonNullable.control('EUR', [
      Validators.required,
      Validators.pattern(/^[A-Za-z]{3}$/)
    ]),
    dateOfRegistration: this.fb.nonNullable.control('', [
      Validators.required,
      isoDateValidator
    ])
  });

  readonly formatShortId = formatShortId;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id || id === 'new') {
      this.isCreate.set(true);
      this.propertyId.set(null);
      return;
    }

    this.isCreate.set(false);
    this.propertyId.set(id);
    this.loadProperty(id);
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

    if (this.isCreate()) {
      this.create(payload);
      return;
    }

    const id = this.propertyId();
    if (!id) {
      this.saving.set(false);
      this.formError.set('Property id is missing.');
      return;
    }

    this.update(id, payload);
  }

  cancel(): void {
    void this.router.navigate(['/properties']);
  }

  controlError(controlName: string): string | null {
    const control = this.form.get(controlName);
    if (!control || !control.touched || !control.errors) {
      return null;
    }

    if (control.errors['required']) {
      return 'This field is required.';
    }

    if (control.errors['maxlength']) {
      return `Must be ${control.errors['maxlength'].requiredLength} characters or fewer.`;
    }

    if (control.errors['min']) {
      return 'Price cannot be negative.';
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

  private loadProperty(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.propertyApi.getPropertyById(id).subscribe({
      next: (property) => {
        this.patchForm(property);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.loading.set(false);
        if (err instanceof ApiError && err.status === 404) {
          this.loadError.set('Property not found.');
          return;
        }

        this.loadError.set('Unable to load this property. Please try again.');
      }
    });
  }

  private create(payload: CreatePropertyRequest): void {
    this.propertyApi.createProperty(payload).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/properties'], {
          state: { message: 'Property created successfully.' }
        });
      },
      error: (err: unknown) => this.handleSaveError(err)
    });
  }

  private update(id: string, payload: UpdatePropertyRequest): void {
    this.propertyApi.updateProperty(id, payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set('Property updated successfully.');
        void this.router.navigate(['/properties'], {
          state: { message: 'Property updated successfully.' }
        });
      },
      error: (err: unknown) => this.handleSaveError(err)
    });
  }

  private handleSaveError(err: unknown): void {
    this.saving.set(false);

    if (!(err instanceof ApiError)) {
      this.formError.set('Unable to save the property. Please try again.');
      return;
    }

    if (err.status === 404) {
      this.formError.set('Property not found.');
      return;
    }

    if (err.status === 409) {
      this.formError.set(err.message || 'A conflict prevented saving this property.');
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

    this.formError.set(err.message || 'Unable to save the property. Please try again.');
  }

  private patchForm(property: PropertyDto): void {
    this.form.reset({
      name: property.name,
      address: property.address,
      price: property.price,
      currency: property.currency,
      dateOfRegistration: property.dateOfRegistration
    });
  }

  private buildPayload(): CreatePropertyRequest {
    const raw = this.form.getRawValue();
    return {
      name: String(raw.name ?? '').trim(),
      address: String(raw.address ?? '').trim(),
      price: Number(raw.price),
      currency: String(raw.currency ?? '')
        .trim()
        .toUpperCase(),
      dateOfRegistration: String(raw.dateOfRegistration ?? '')
    };
  }
}

function isoDateValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '').trim();
  if (!value) {
    return null;
  }

  return /^\d{4}-\d{2}-\d{2}$/.test(value) ? null : { isoDate: true };
}
