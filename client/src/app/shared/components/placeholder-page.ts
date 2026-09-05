import { Component, Input } from '@angular/core';

/** Simple placeholder used until feature UIs are implemented. */
@Component({
  selector: 'app-placeholder-page',
  standalone: true,
  template: `
    <section class="placeholder">
      <h1>{{ title }}</h1>
      <p>{{ description }}</p>
    </section>
  `,
  styles: `
    .placeholder {
      padding: 1.5rem;
      background: #f7f8fa;
      border: 1px solid #e3e6eb;
      border-radius: 8px;
    }

    h1 {
      margin: 0 0 0.5rem;
      font-size: 1.5rem;
    }

    p {
      margin: 0;
      color: #4b5563;
    }
  `
})
export class PlaceholderPage {
  @Input({ required: true }) title!: string;
  @Input() description = 'This feature UI will be implemented in a later slice.';
}
