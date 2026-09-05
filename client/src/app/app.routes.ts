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
      import('./features/properties/properties-page').then((m) => m.PropertiesPage)
  },
  {
    path: 'contacts',
    loadComponent: () =>
      import('./features/contacts/contacts-page').then((m) => m.ContactsPage)
  },
  { path: '**', redirectTo: 'dashboard' }
];
