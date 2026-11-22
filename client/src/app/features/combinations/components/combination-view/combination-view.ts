import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { BigNumberPipe } from '../../../../shared/big-number/big-number.pipe';
import { Combination } from '../../data/combinations.models';

// Displays one combination with its ordinal number.
@Component({
  selector: 'app-combination-view',
  imports: [BigNumberPipe],
  template: `קומבינציה מספר <span class="ltr">{{ combination().ordinal | bigNumber }}</span> –
    <span class="ltr values">{{ formatted() }}</span>`,
  styles: `.values { font-family: ui-monospace, Consolas, monospace; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CombinationView {
  readonly combination = input.required<Combination>();

  protected readonly formatted = computed(() => `[${this.combination().values.join(',')}]`);
}
