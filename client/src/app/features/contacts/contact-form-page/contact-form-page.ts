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
  ContactDto,
  CreateContactRequest,
  UpdateContactRequest
} from '../../../core/models/contact.models';
import { ApiError } from '../../../core/models/problem-details';
import { ContactApiService } from '../../../core/services/contact-api.service';
import { applyApiFieldErrors } from '../../../shared/utils/api-form-errors';
import { formatShortId } from '../../../shared/utils/format';

const DUPLICATE_EMAIL_MESSAGE = 'Another contact already uses this email address.';

@Component({
  selector: 'app-contact-form-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './contact-form-page.html',
  styleUrl: './contact-form-page.css'
})
export class ContactFormPage implements OnInit {
  private readonly contactApi = inject(ContactApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly isCreate = signal(true);
  readonly contactId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);

  readonly form = this.fb.group({
    firstName: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(100)
    ]),
    lastName: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(100)
    ]),
    phoneNumber: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(30),
      phoneValidator
    ]),
    email: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(256),
      Validators.email
    ])
  });

  readonly formatShortId = formatShortId;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id || id === 'new') {
      this.isCreate.set(true);
      this.contactId.set(null);
      return;
    }

    this.isCreate.set(false);
    this.contactId.set(id);
    this.loadContact(id);
  }

  submit(): void {
    this.formError.set(null);
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

    const id = this.contactId();
    if (!id) {
      this.saving.set(false);
      this.formError.set('Contact id is missing.');
      return;
    }

    this.update(id, payload);
  }

  cancel(): void {
    void this.router.navigate(['/contacts']);
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

    if (control.errors['email']) {
      return 'Enter a valid email address.';
    }

    if (control.errors['phone']) {
      return 'Enter a valid phone number.';
    }

    if (control.errors['api']) {
      return control.errors['api'] as string;
    }

    return 'Invalid value.';
  }

  private loadContact(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);

    this.contactApi.getContactById(id).subscribe({
      next: (contact) => {
        this.patchForm(contact);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.loading.set(false);
        if (err instanceof ApiError && err.status === 404) {
          this.loadError.set('Contact not found.');
          return;
        }

        this.loadError.set('Unable to load this contact. Please try again.');
      }
    });
  }

  private create(payload: CreateContactRequest): void {
    this.contactApi.createContact(payload).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/contacts'], {
          state: { message: 'Contact created successfully.' }
        });
      },
      error: (err: unknown) => this.handleSaveError(err)
    });
  }

  private update(id: string, payload: UpdateContactRequest): void {
    this.contactApi.updateContact(id, payload).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/contacts'], {
          state: { message: 'Contact updated successfully.' }
        });
      },
      error: (err: unknown) => this.handleSaveError(err)
    });
  }

  private handleSaveError(err: unknown): void {
    this.saving.set(false);

    if (!(err instanceof ApiError)) {
      this.formError.set('Unable to save the contact. Please try again.');
      return;
    }

    if (err.status === 404) {
      this.formError.set('Contact not found.');
      return;
    }

    if (err.status === 409) {
      this.applyEmailConflict();
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

    this.formError.set(err.message || 'Unable to save the contact. Please try again.');
  }

  private applyEmailConflict(): void {
    const email = this.form.get('email');
    if (email) {
      email.setErrors({ ...(email.errors ?? {}), api: DUPLICATE_EMAIL_MESSAGE });
      email.markAsTouched();
    }

    this.formError.set(DUPLICATE_EMAIL_MESSAGE);
  }

  private patchForm(contact: ContactDto): void {
    this.form.reset({
      firstName: contact.firstName,
      lastName: contact.lastName,
      phoneNumber: contact.phoneNumber,
      email: contact.email
    });
  }

  private buildPayload(): CreateContactRequest {
    const raw = this.form.getRawValue();
    return {
      firstName: String(raw.firstName ?? '').trim(),
      lastName: String(raw.lastName ?? '').trim(),
      phoneNumber: String(raw.phoneNumber ?? '').trim(),
      email: String(raw.email ?? '').trim()
    };
  }
}

function phoneValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '').trim();
  if (!value) {
    return null;
  }

  return /^[\d\s+\-().]{1,30}$/.test(value) ? null : { phone: true };
}
