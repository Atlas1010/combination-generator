import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';

import { toUserMessage } from '../../../core/http/api-error';
import { readTabItem, writeTabItem } from '../../../core/storage/tab-storage';
import { CombinationsApi } from '../data/combinations-api';
import { Combination, CombinationPage, GenerationSession } from '../data/combinations.models';

export type ViewMode = 'step' | 'all';

export const PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;

interface CombinationsState {
  session: GenerationSession | null;
  mode: ViewMode;
  current: Combination | null;
  page: CombinationPage | null;
  pageSize: number;
  notice: string | null;
  error: string | null;
  loading: boolean;
}

const initialState: CombinationsState = {
  session: null,
  mode: 'step',
  current: null,
  page: null,
  pageSize: PAGE_SIZE_OPTIONS[1],
  notice: null,
  error: null,
  loading: false,
};

const STORAGE_KEY = 'combinator.state';

// The part of the state that survives a page refresh.
interface PersistedState {
  session: GenerationSession;
  current: Combination | null;
  pageSize: number;
}

// Feature store holding the combinations UI state.
@Injectable({ providedIn: 'root' })
export class CombinationsStore {
  private readonly api = inject(CombinationsApi);
  private readonly state = signal<CombinationsState>({ ...initialState, ...restoreState() });

  constructor() {
    effect(() => {
      const { session, mode, current, page, pageSize } = this.state();
      const lastSeen = mode === 'all' ? (page?.items.at(-1) ?? current) : current;
      writeTabItem(STORAGE_KEY, session ? ({ session, current: lastSeen, pageSize } satisfies PersistedState) : null);
    });
  }

  readonly session = computed(() => this.state().session);
  readonly hasSession = computed(() => this.state().session !== null);
  readonly mode = computed(() => this.state().mode);
  readonly current = computed(() => this.state().current);
  readonly page = computed(() => this.state().page);
  readonly pageSize = computed(() => this.state().pageSize);
  readonly notice = computed(() => this.state().notice);
  readonly error = computed(() => this.state().error);
  readonly loading = computed(() => this.state().loading);

  async start(n: number): Promise<boolean> {
    this.state.set({ ...initialState, pageSize: this.state().pageSize });
    const session = await this.run(this.api.start(n));
    if (session) {
      this.patch({ session });
    }
    return session !== null;
  }

  async next(): Promise<void> {
    const session = this.requireSession();
    const result = await this.run(this.api.next(session.id));
    if (result) {
      this.patch({ current: result.combination ?? this.state().current, notice: result.message });
    }
  }

  // Enters "all" mode starting after the current combination.
  async showAll(): Promise<void> {
    const session = this.requireSession();
    const page = await this.run(this.api.getPage(session.id, { pageSize: this.state().pageSize, pageNumber: 1n }));
    if (page) {
      this.patch({ mode: 'all', page, notice: page.message });
    }
  }

  async goToPage(pageNumber: bigint): Promise<void> {
    const { session, page, pageSize } = this.state();
    if (!session || !page) {
      return;
    }
    await this.loadPage(session, { pageSize, pageNumber, anchor: page.anchor });
  }

  async changePageSize(pageSize: number): Promise<void> {
    const { session, page } = this.state();
    this.patch({ pageSize });
    if (session && page) {
      await this.loadPage(session, { pageSize, pageNumber: 1n, anchor: page.anchor });
    }
  }

  // Returns to "step" mode at the last combination shown.
  back(): void {
    const { page, current } = this.state();
    this.patch({ mode: 'step', current: page?.items.at(-1) ?? current, page: null, notice: null, error: null });
  }

  reset(): void {
    this.state.set({ ...initialState, pageSize: this.state().pageSize });
  }

  private async loadPage(
    session: GenerationSession,
    request: { pageSize: number; pageNumber: bigint; anchor: bigint },
  ): Promise<void> {
    const page = await this.run(this.api.getPage(session.id, request));
    if (page) {
      this.patch({ page, notice: page.message });
    }
  }

  // Runs one request with loading and error bookkeeping.
  private async run<T>(request: Observable<T>): Promise<T | null> {
    this.patch({ loading: true, error: null });
    try {
      return await firstValueFrom(request);
    } catch (error) {
      this.patch({ error: toUserMessage(error) });
      return null;
    } finally {
      this.patch({ loading: false });
    }
  }

  private requireSession(): GenerationSession {
    const session = this.state().session;
    if (!session) {
      throw new Error('No active session. Call start() first.');
    }
    return session;
  }

  private patch(partial: Partial<CombinationsState>): void {
    this.state.update((state) => ({ ...state, ...partial }));
  }
}

// Reads the persisted state, treating malformed data as absent.
function restoreState(): Partial<CombinationsState> {
  const stored = readTabItem(STORAGE_KEY) as Partial<PersistedState> | null;
  const session = stored?.session;
  if (!session || typeof session.id !== 'string' || typeof session.n !== 'number' || typeof session.totalCount !== 'bigint') {
    return {};
  }

  const current = stored.current;
  const validCurrent =
    current && typeof current.ordinal === 'bigint' && Array.isArray(current.values) ? current : null;
  const pageSize = (PAGE_SIZE_OPTIONS as readonly number[]).includes(stored.pageSize ?? -1)
    ? stored.pageSize!
    : initialState.pageSize;

  return { session, current: validCurrent, pageSize };
}
