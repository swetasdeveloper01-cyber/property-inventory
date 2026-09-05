import { Component } from '@angular/core';
import { PlaceholderPage } from '../../shared/components/placeholder-page';

@Component({
  selector: 'app-contacts-page',
  standalone: true,
  imports: [PlaceholderPage],
  template: `
    <app-placeholder-page
      title="Contacts"
      description="Contact screens will be implemented in a later slice. ContactApiService is ready for integration."
    />
  `
})
export class ContactsPage {}
