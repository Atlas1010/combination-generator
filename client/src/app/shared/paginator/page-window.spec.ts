import { PageLink, buildPageWindow, parsePageNumber } from './page-window';

const describeLinks = (links: PageLink[]) => links.map((link) => (link.kind === 'page' ? link.page.toString() : '…'));

describe('buildPageWindow', () => {
  it('lists every page when there are only a few', () => {
    expect(describeLinks(buildPageWindow(2n, 4n))).toEqual(['1', '2', '3', '4']);
  });

  it('shows first, last and a window around the current page', () => {
    expect(describeLinks(buildPageWindow(50n, 100n))).toEqual(['1', '…', '48', '49', '50', '51', '52', '…', '100']);
  });

  it('handles page counts beyond Number.MAX_SAFE_INTEGER', () => {
    const total = 243_290_200_817_664_000n;
    expect(describeLinks(buildPageWindow(total, total))).toEqual([
      '1',
      '…',
      '243290200817663998',
      '243290200817663999',
      '243290200817664000',
    ]);
  });

  it('returns nothing when there are no pages', () => {
    expect(buildPageWindow(1n, 0n)).toEqual([]);
  });
});

describe('parsePageNumber', () => {
  it('accepts numbers inside the range, with or without thousands separators', () => {
    expect(parsePageNumber('7', 10n)).toBe(7n);
    expect(parsePageNumber('1,000', 5000n)).toBe(1000n);
  });

  it('rejects out-of-range or non-numeric input', () => {
    expect(parsePageNumber('0', 10n)).toBeNull();
    expect(parsePageNumber('11', 10n)).toBeNull();
    expect(parsePageNumber('abc', 10n)).toBeNull();
    expect(parsePageNumber('-3', 10n)).toBeNull();
  });
});
