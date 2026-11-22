import { ChangeDetectionStrategy, Component, input } from '@angular/core';

// Inline status message announced to screen readers.
@Component({
  selector: 'app-alert',
  template: `
    @if (message(); as text) {
      <p class="alert" [class]="kind()" [attr.role]="kind() === 'error' ? 'alert' : 'status'">{{ text }}</p>
    }
  `,
  styles: `
    .alert { margin: 0.75rem 0; padding: 0.625rem 0.875rem; border-radius: 6px; border: 1px solid; }
    .error { color: var(--danger); background: #fdecec; border-color: #f5c2c2; }
    .info { color: #234; background: #eef5ff; border-color: #c7dcf7; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Alert {
  readonly message = input<string | null>(null);
  readonly kind = input<'info' | 'error'>('info');
}
