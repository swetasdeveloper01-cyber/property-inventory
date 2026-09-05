import { Component } from '@angular/core';
import { PlaceholderPage } from '../../shared/components/placeholder-page';

@Component({
  selector: 'app-properties-page',
  standalone: true,
  imports: [PlaceholderPage],
  template: `
    <app-placeholder-page
      title="Properties"
      description="Property screens will be implemented in a later slice. PropertyApiService is ready for integration."
    />
  `
})
export class PropertiesPage {}
