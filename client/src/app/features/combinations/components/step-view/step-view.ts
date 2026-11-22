import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { Combination } from '../../data/combinations.models';
import { CombinationView } from '../combination-view/combination-view';

// Shows the current combination and the step actions.
@Component({
  selector: 'app-step-view',
  imports: [CombinationView],
  template: `
    <div class="current" aria-live="polite">
      @if (current(); as combination) {
        <app-combination-view [combination]="combination" />
      } @else {
        <span class="muted">לחצ/י על "הבא" כדי להציג את הקומבינציה הראשונה.</span>
      }
    </div>

    <div class="actions">
      <button type="button" (click)="reset.emit()" [disabled]="busy()">איפוס</button>
      <button type="button" class="primary" (click)="next.emit()" [disabled]="busy()">הבא</button>
      <button type="button" (click)="showAll.emit()" [disabled]="busy()">כל הקומבינציות</button>
    </div>
  `,
  styles: `
    .current { min-height: 2.5rem; font-size: 1.25rem; padding: 0.75rem 0; }
    .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepView {
  readonly current = input<Combination | null>(null);
  readonly busy = input(false);
  readonly reset = output();
  readonly next = output();
  readonly showAll = output();
}
