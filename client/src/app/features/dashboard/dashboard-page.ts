import { Component } from '@angular/core';
import { PlaceholderPage } from '../../shared/components/placeholder-page';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [PlaceholderPage],
  template: `
    <app-placeholder-page
      title="Sales Dashboard"
      description="Dashboard UI will be implemented in the next frontend slice. API integration is available via DashboardApiService."
    />
  `
})
export class DashboardPage {}
