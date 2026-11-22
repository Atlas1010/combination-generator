import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import { BigNumberPipe } from '../big-number/big-number.pipe';
import { buildPageWindow, parsePageNumber } from './page-window';

// Stateless pager that emits the page the user selects.
@Component({
  selector: 'app-paginator',
  imports: [BigNumberPipe],
  templateUrl: './paginator.html',
  styleUrl: './paginator.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Paginator {
  readonly currentPage = input.required<bigint>();
  readonly totalPages = input.required<bigint>();
  readonly disabled = input(false);
  readonly pageChange = output<bigint>();

  protected readonly links = computed(() => buildPageWindow(this.currentPage(), this.totalPages()));
  protected readonly isFirst = computed(() => this.currentPage() <= 1n);
  protected readonly isLast = computed(() => this.currentPage() >= this.totalPages());
  protected readonly jumpError = signal(false);

  protected first(): void {
    this.go(1n);
  }

  protected previous(): void {
    this.go(this.currentPage() - 1n);
  }

  protected next(): void {
    this.go(this.currentPage() + 1n);
  }

  protected last(): void {
    this.go(this.totalPages());
  }

  protected go(page: bigint): void {
    if (!this.disabled() && page !== this.currentPage() && page >= 1n && page <= this.totalPages()) {
      this.pageChange.emit(page);
    }
  }

  protected jump(input: HTMLInputElement): void {
    const page = parsePageNumber(input.value, this.totalPages());
    this.jumpError.set(page === null);
    if (page !== null) {
      this.go(page);
      input.value = '';
    }
  }
}
