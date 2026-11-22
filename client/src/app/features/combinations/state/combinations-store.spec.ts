import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CombinationsStore } from './combinations-store';

describe('CombinationsStore', () => {
  let store: CombinationsStore;
  let http: HttpTestingController;

  const flushNextTick = () => new Promise((resolve) => setTimeout(resolve));

  // Builds a fresh app instance, like a browser page load.
  function loadPage(): void {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(CombinationsStore);
    http = TestBed.inject(HttpTestingController);
  }

  beforeEach(() => {
    sessionStorage.clear();
    loadPage();
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  async function startSession(): Promise<void> {
    const started = store.start(3);
    http.expectOne('/api/combinations/start').flush({ sessionId: 's1', n: 3, totalCount: '6' });
    await started;
  }

  it('keeps the total count exact as a bigint', async () => {
    const started = store.start(20);
    http.expectOne('/api/combinations/start').flush({ sessionId: 's1', n: 20, totalCount: '2432902008176640000' });
    await started;

    expect(store.session()?.totalCount).toBe(2_432_902_008_176_640_000n);
  });

  it('shows the next combination returned by the server', async () => {
    await startSession();

    const next = store.next();
    http.expectOne('/api/combinations/s1/next').flush({
      combination: { ordinal: '1', values: [1, 2, 3] },
      hasMore: true,
      totalCount: '6',
      message: null,
    });
    await next;

    expect(store.current()).toEqual({ ordinal: 1n, values: [1, 2, 3] });
  });

  it('keeps the last combination and shows the server message when there are no more', async () => {
    await startSession();
    const first = store.next();
    http.expectOne('/api/combinations/s1/next').flush({
      combination: { ordinal: '6', values: [3, 2, 1] }, hasMore: false, totalCount: '6', message: null,
    });
    await first;

    const none = store.next();
    http.expectOne('/api/combinations/s1/next').flush({
      combination: null, hasMore: false, totalCount: '6', message: 'אין יותר קומבינציות',
    });
    await none;

    expect(store.current()?.ordinal).toBe(6n);
    expect(store.notice()).toBe('אין יותר קומבינציות');
  });

  it('pages from the anchor and returns to the last combination shown on "back"', async () => {
    await startSession();

    const all = store.showAll();
    const firstRequest = http.expectOne('/api/combinations/s1/all');
    expect(firstRequest.request.body).toEqual({ pageSize: 20, pageNumber: '1' });
    firstRequest.flush({
      items: [{ ordinal: '1', values: [1, 2, 3] }, { ordinal: '2', values: [1, 3, 2] }],
      anchor: '0', pageNumber: '1', pageSize: 2, totalPages: '3', totalItems: '6', totalCount: '6', message: null,
    });
    await all;
    expect(store.mode()).toBe('all');

    const page = store.goToPage(2n);
    const pageRequest = http.expectOne('/api/combinations/s1/all');
    expect(pageRequest.request.body).toEqual({ pageSize: 20, pageNumber: '2', anchor: '0' });
    pageRequest.flush({
      items: [{ ordinal: '3', values: [2, 1, 3] }, { ordinal: '4', values: [2, 3, 1] }],
      anchor: '0', pageNumber: '2', pageSize: 2, totalPages: '3', totalItems: '6', totalCount: '6', message: null,
    });
    await page;

    store.back();

    expect(store.mode()).toBe('step');
    expect(store.current()?.ordinal).toBe(4n);
  });

  it('survives a page refresh on the last combination shown', async () => {
    await startSession();
    const next = store.next();
    http.expectOne('/api/combinations/s1/next').flush({
      combination: { ordinal: '2', values: [1, 3, 2] }, hasMore: true, totalCount: '6', message: null,
    });
    await next;
    TestBed.tick();

    loadPage();

    expect(store.session()).toEqual({ id: 's1', n: 3, totalCount: 6n });
    expect(store.current()).toEqual({ ordinal: 2n, values: [1, 3, 2] });
    expect(store.mode()).toBe('step');
  });

  it('forgets the session after reset', async () => {
    await startSession();
    TestBed.tick();

    store.reset();
    TestBed.tick();
    loadPage();

    expect(store.hasSession()).toBeFalse();
  });

  it('ignores malformed persisted state', () => {
    sessionStorage.setItem('combinator.state', '{"session":{"id":42}}');

    loadPage();

    expect(store.hasSession()).toBeFalse();
  });

  it('turns a failed request into a user-facing error', async () => {
    await startSession();

    const next = store.next();
    http.expectOne('/api/combinations/s1/next').flush(null, { status: 404, statusText: 'Not Found' });
    await next;
    await flushNextTick();

    expect(store.error()).toContain('איפוס');
    expect(store.loading()).toBeFalse();
  });
});
