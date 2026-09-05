import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../core/models/problem-details';
import { ContactDto } from '../../../core/models/contact.models';
import { ContactApiService } from '../../../core/services/contact-api.service';
import { ContactFormPage } from './contact-form-page';

describe('ContactFormPage', () => {
  let fixture: ComponentFixture<ContactFormPage>;
  let api: {
    getContactById: ReturnType<typeof vi.fn>;
    createContact: ReturnType<typeof vi.fn>;
    updateContact: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  const existing: ContactDto = {
    id: 'c1111111-1111-1111-1111-111111111111',
    firstName: 'Carmen',
    lastName: 'Attard',
    phoneNumber: '+356 2123 4567',
    email: 'carmen.attard@example.com'
  };

  async function setup(mode: 'create' | 'edit', options?: { loadError?: unknown }) {
    TestBed.resetTestingModule();

    api = {
      getContactById: vi.fn(() =>
        options?.loadError ? throwError(() => options.loadError) : of(existing)
      ),
      createContact: vi.fn(() => of({ ...existing, id: 'new-id' })),
      updateContact: vi.fn(() => of(existing))
    };

    const paramMap =
      mode === 'create'
        ? convertToParamMap({})
        : convertToParamMap({ id: existing.id });

    await TestBed.configureTestingModule({
      imports: [ContactFormPage],
      providers: [
        { provide: ContactApiService, useValue: api },
        provideRouter([{ path: 'contacts', component: ContactFormPage }]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ContactFormPage);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  }

  function fillValidForm(): void {
    fixture.componentInstance.form.setValue({
      firstName: 'Joe',
      lastName: 'Borg',
      phoneNumber: '+356 9988 7766',
      email: 'joe.borg@example.com'
    });
  }

  describe('create', () => {
    beforeEach(async () => {
      await setup('create');
    });

    it('renders the create form', () => {
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('h1')?.textContent).toContain('Add Contact');
      expect(fixture.nativeElement.querySelector('#contact-first-name')).toBeTruthy();
    });

    it('required validation works', () => {
      fixture.detectChanges();
      const component = fixture.componentInstance;
      component.form.reset({
        firstName: '',
        lastName: '',
        phoneNumber: '',
        email: ''
      });
      component.submit();
      fixture.detectChanges();

      expect(api.createContact).not.toHaveBeenCalled();
      expect(component.controlError('firstName')).toContain('required');
      expect(component.controlError('lastName')).toContain('required');
      expect(component.controlError('email')).toContain('required');
    });

    it('invalid email cannot be submitted', () => {
      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.form.patchValue({ email: 'not-an-email' });
      fixture.componentInstance.submit();

      expect(api.createContact).not.toHaveBeenCalled();
      expect(fixture.componentInstance.controlError('email')).toContain('valid email');
    });

    it('invalid form does not call API', () => {
      fixture.detectChanges();
      fixture.componentInstance.submit();
      expect(api.createContact).not.toHaveBeenCalled();
    });

    it('valid form calls create API with expected payload', () => {
      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.submit();

      expect(api.createContact).toHaveBeenCalledWith({
        firstName: 'Joe',
        lastName: 'Borg',
        phoneNumber: '+356 9988 7766',
        email: 'joe.borg@example.com'
      });
    });

    it('successful creation navigates to the list', async () => {
      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.submit();
      await fixture.whenStable();

      expect(router.navigate).toHaveBeenCalledWith(['/contacts'], {
        state: { message: 'Contact created successfully.' }
      });
    });

    it('API validation error is displayed', async () => {
      api.createContact.mockReturnValue(
        throwError(
          () =>
            new ApiError(400, {
              title: 'Validation failed',
              detail: 'One or more validation errors occurred.',
              errors: { FirstName: ['FirstName is required.'] }
            })
        )
      );

      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.banner-error')?.textContent).toContain(
        'validation'
      );
      expect(fixture.componentInstance.controlError('firstName')).toBe('FirstName is required.');
    });

    it('409 duplicate email is displayed on the email field', async () => {
      api.createContact.mockReturnValue(
        throwError(
          () =>
            new ApiError(409, {
              title: 'Conflict',
              detail: "A contact with email 'joe.borg@example.com' already exists."
            })
        )
      );

      fixture.detectChanges();
      fillValidForm();
      fixture.componentInstance.submit();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.banner-error')?.textContent).toContain(
        'Another contact already uses this email address.'
      );
      expect(fixture.componentInstance.controlError('email')).toBe(
        'Another contact already uses this email address.'
      );
      expect(api.createContact).toHaveBeenCalledTimes(1);
    });
  });

  describe('edit', () => {
    beforeEach(async () => {
      await setup('edit');
    });

    it('loads the contact by ID', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      expect(api.getContactById).toHaveBeenCalledWith(existing.id);
    });

    it('existing values populate the form', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.componentInstance.form.getRawValue()).toEqual({
        firstName: 'Carmen',
        lastName: 'Attard',
        phoneNumber: '+356 2123 4567',
        email: 'carmen.attard@example.com'
      });
    });

    it('update sends the expected payload', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ phoneNumber: '+356 1111 2222' });
      fixture.componentInstance.submit();

      expect(api.updateContact).toHaveBeenCalledWith(existing.id, {
        firstName: 'Carmen',
        lastName: 'Attard',
        phoneNumber: '+356 1111 2222',
        email: 'carmen.attard@example.com'
      });
    });

    it('missing contact / API 404 displays an error', async () => {
      await setup('edit', {
        loadError: new ApiError(404, { title: 'Not Found', detail: 'Contact not found.' })
      });

      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.state-error')?.textContent).toContain(
        'Contact not found'
      );
      expect(fixture.nativeElement.querySelector('.contact-form')).toBeNull();
    });

    it('successful update provides navigation feedback', async () => {
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      fixture.componentInstance.submit();
      await fixture.whenStable();

      expect(router.navigate).toHaveBeenCalledWith(['/contacts'], {
        state: { message: 'Contact updated successfully.' }
      });
    });

    it('save button is disabled while saving', async () => {
      api.updateContact.mockReturnValue({
        subscribe: () => ({ unsubscribe() {} })
      });

      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();

      fixture.componentInstance.submit();
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector(
        'button[type="submit"]'
      ) as HTMLButtonElement;
      expect(button.disabled).toBe(true);
      expect(button.textContent).toContain('Saving');
    });
  });
});
