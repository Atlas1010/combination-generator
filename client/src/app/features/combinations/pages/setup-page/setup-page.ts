import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { Alert } from '../../../../shared/alert/alert';
import { SizeForm } from '../../components/size-form/size-form';
import { CombinationsStore } from '../../state/combinations-store';

// First screen: asks for n and starts a session.
@Component({
  selector: 'app-setup-page',
  imports: [SizeForm, Alert],
  template: `
    <section class="card">
      <app-size-form [disabled]="store.loading()" (submitted)="start($event)" />
      <app-alert kind="error" [message]="store.error()" />
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetupPage {
  protected readonly store = inject(CombinationsStore);
  private readonly router = inject(Router);

  protected async start(n: number): Promise<void> {
    if (await this.store.start(n)) {
      await this.router.navigate(['/generator']);
    }
  }
}
