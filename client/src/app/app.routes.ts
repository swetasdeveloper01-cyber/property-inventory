import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard-page').then((m) => m.DashboardPage)
  },
  {
    path: 'properties',
    loadComponent: () =>
      import('./features/properties/property-list-page/property-list-page').then(
        (m) => m.PropertyListPage
      )
  },
  {
    path: 'properties/new',
    loadComponent: () =>
      import('./features/properties/property-form-page/property-form-page').then(
        (m) => m.PropertyFormPage
      )
  },
  {
    path: 'properties/:id',
    loadComponent: () =>
      import('./features/properties/property-form-page/property-form-page').then(
        (m) => m.PropertyFormPage
      )
  },
  {
    path: 'contacts',
    loadComponent: () =>
      import('./features/contacts/contact-list-page/contact-list-page').then(
        (m) => m.ContactListPage
      )
  },
  {
    path: 'contacts/new',
    loadComponent: () =>
      import('./features/contacts/contact-form-page/contact-form-page').then(
        (m) => m.ContactFormPage
      )
  },
  {
    path: 'contacts/:id',
    loadComponent: () =>
      import('./features/contacts/contact-form-page/contact-form-page').then(
        (m) => m.ContactFormPage
      )
  },
  { path: '**', redirectTo: 'dashboard' }
];
