import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { Alert } from '../../../../shared/alert/alert';
import { AllView } from '../../components/all-view/all-view';
import { StepView } from '../../components/step-view/step-view';
import { TotalSummary } from '../../components/total-summary/total-summary';
import { CombinationsStore, PAGE_SIZE_OPTIONS } from '../../state/combinations-store';

// Container page for an active generation session.
@Component({
  selector: 'app-generator-page',
  imports: [Alert, AllView, StepView, TotalSummary],
  template: `
    @if (store.session(); as session) {
      <section class="card">
        <app-total-summary [n]="session.n" [total]="session.totalCount" />

        @switch (store.mode()) {
          @case ('step') {
            <app-step-view
              [current]="store.current()"
              [busy]="store.loading()"
              (reset)="reset()"
              (next)="store.next()"
              (showAll)="store.showAll()" />
          }
          @case ('all') {
            @if (store.page(); as page) {
              <app-all-view
                [page]="page"
                [pageSize]="store.pageSize()"
                [pageSizeOptions]="pageSizeOptions"
                [busy]="store.loading()"
                (pageChange)="store.goToPage($event)"
                (pageSizeChange)="store.changePageSize($event)"
                (back)="store.back()" />
            }
          }
        }

        <app-alert kind="info" [message]="store.notice()" />
        <app-alert kind="error" [message]="store.error()" />
      </section>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GeneratorPage {
  protected readonly store = inject(CombinationsStore);
  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;
  private readonly router = inject(Router);

  protected async reset(): Promise<void> {
    this.store.reset();
    await this.router.navigate(['/']);
  }
}
