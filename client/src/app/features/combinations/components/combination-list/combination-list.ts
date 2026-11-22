import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { Combination } from '../../data/combinations.models';
import { CombinationView } from '../combination-view/combination-view';

@Component({
  selector: 'app-combination-list',
  imports: [CombinationView],
  template: `
    <ul class="list">
      @for (item of items(); track item.ordinal) {
        <li><app-combination-view [combination]="item" /></li>
      }
    </ul>
  `,
  styles: `
    .list { list-style: none; margin: 0.75rem 0; padding: 0; columns: 2 22rem; column-gap: 2rem; }
    li { padding: 0.25rem 0; border-bottom: 1px solid var(--border); break-inside: avoid; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CombinationList {
  readonly items = input.required<readonly Combination[]>();
}
