import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { BigNumberPipe } from '../../../../shared/big-number/big-number.pipe';

@Component({
  selector: 'app-total-summary',
  imports: [BigNumberPipe],
  template: `
    <p class="summary">
      סה"כ האפשרויות לסדר {{ n() }} מספרים בשורה הוא:
      <strong class="ltr">{{ total() | bigNumber }}</strong>
    </p>
  `,
  styles: `.summary { font-size: 1.125rem; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TotalSummary {
  readonly n = input.required<number>();
  readonly total = input.required<bigint>();
}
