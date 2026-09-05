import { applyApiFieldErrors } from './api-form-errors';

describe('applyApiFieldErrors', () => {
  it('maps PascalCase API keys onto camelCase form controls', () => {
    const applied: Record<string, string> = {};
    applyApiFieldErrors(
      {
        Name: ['Name is required.'],
        DateOfRegistration: ['DateOfRegistration is required.']
      },
      (control, message) => {
        applied[control] = message;
      }
    );

    expect(applied).toEqual({
      name: 'Name is required.',
      dateOfRegistration: 'DateOfRegistration is required.'
    });
  });
});
