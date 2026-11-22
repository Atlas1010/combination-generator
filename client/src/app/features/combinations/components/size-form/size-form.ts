import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

export const MIN_N = 1;
export const MAX_N = 20;

// Form that collects n, the number of values to arrange.
@Component({
  selector: 'app-size-form',
  imports: [ReactiveFormsModule],
  template: `
    <form (submit)="$event.preventDefault(); submit()">
      <label for="n">כמה מספרים לסדר בשורה? (n בין {{ min }} ל-{{ max }})</label>
      <div class="row">
        <input id="n" type="number" [min]="min" [max]="max" step="1" [formControl]="n" autofocus />
        <button type="submit" class="primary" [disabled]="disabled()">התחל</button>
      </div>
      @if (n.invalid && (n.dirty || n.touched)) {
        <p class="field-error" role="alert">יש להזין מספר שלם בין {{ min }} ל-{{ max }}.</p>
      }
    </form>
  `,
  styles: `
    .row { display: flex; gap: 0.5rem; margin-top: 0.5rem; }
    input { width: 7rem; }
    .field-error { color: var(--danger); margin: 0.5rem 0 0; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SizeForm {
  readonly disabled = input(false);
  readonly submitted = output<number>();

  protected readonly min = MIN_N;
  protected readonly max = MAX_N;
  protected readonly n = new FormControl<number | null>(null, [
    Validators.required,
    Validators.min(MIN_N),
    Validators.max(MAX_N),
    Validators.pattern(/^\d+$/),
  ]);

  protected submit(): void {
    this.n.markAsTouched();
    if (this.n.valid && this.n.value !== null) {
      this.submitted.emit(this.n.value);
    }
  }
}
