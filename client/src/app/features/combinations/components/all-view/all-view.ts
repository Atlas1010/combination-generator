import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { BigNumberPipe } from '../../../../shared/big-number/big-number.pipe';
import { Paginator } from '../../../../shared/paginator/paginator';
import { CombinationPage } from '../../data/combinations.models';
import { CombinationList } from '../combination-list/combination-list';

// Shows a page of combinations with a pager.
@Component({
  selector: 'app-all-view',
  imports: [BigNumberPipe, CombinationList, Paginator],
  template: `
    <div class="toolbar">
      <button type="button" (click)="back.emit()" [disabled]="busy()">חזור</button>
      <label>
        קומבינציות בדף
        <select [value]="pageSize()" (change)="onPageSizeChange($event)" [disabled]="busy()">
          @for (size of pageSizeOptions(); track size) {
            <option [value]="size" [selected]="size === pageSize()">{{ size }}</option>
          }
        </select>
      </label>
      @if (hasPages()) {
        <span class="muted">
          דף <span class="ltr">{{ page().pageNumber | bigNumber }}</span> מתוך
          <span class="ltr">{{ page().totalPages | bigNumber }}</span>
        </span>
      }
    </div>

    @if (page().items.length > 0) {
      <app-combination-list [items]="page().items" />
      <app-paginator
        [currentPage]="page().pageNumber"
        [totalPages]="page().totalPages"
        [disabled]="busy()"
        (pageChange)="pageChange.emit($event)" />
    }
  `,
  styles: `
    .toolbar { display: flex; align-items: center; gap: 1rem; flex-wrap: wrap; }
    select { margin-inline-start: 0.375rem; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AllView {
  readonly page = input.required<CombinationPage>();
  readonly pageSize = input.required<number>();
  readonly pageSizeOptions = input.required<readonly number[]>();
  readonly busy = input(false);
  readonly pageChange = output<bigint>();
  readonly pageSizeChange = output<number>();
  readonly back = output();

  protected readonly hasPages = computed(() => this.page().totalPages > 0n);

  protected onPageSizeChange(event: Event): void {
    this.pageSizeChange.emit(Number((event.target as HTMLSelectElement).value));
  }
}
