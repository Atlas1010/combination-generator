export type PageLink = { readonly kind: 'page'; readonly page: bigint } | { readonly kind: 'gap'; readonly id: string };

// Builds the page links around the current page, with gaps.
export function buildPageWindow(current: bigint, totalPages: bigint, radius = 2n): PageLink[] {
  if (totalPages < 1n) {
    return [];
  }

  const from = current - radius > 1n ? current - radius : 1n;
  const to = current + radius < totalPages ? current + radius : totalPages;
  const links: PageLink[] = [];

  if (from > 1n) {
    links.push({ kind: 'page', page: 1n });
    if (from > 2n) {
      links.push({ kind: 'gap', id: 'start-gap' });
    }
  }

  for (let page = from; page <= to; page++) {
    links.push({ kind: 'page', page });
  }

  if (to < totalPages) {
    if (to < totalPages - 1n) {
      links.push({ kind: 'gap', id: 'end-gap' });
    }
    links.push({ kind: 'page', page: totalPages });
  }

  return links;
}

// Parses user input into a valid page number, or null.
export function parsePageNumber(input: string, totalPages: bigint): bigint | null {
  const trimmed = input.trim().replaceAll(',', '');
  if (!/^\d{1,19}$/.test(trimmed)) {
    return null;
  }
  const page = BigInt(trimmed);
  return page >= 1n && page <= totalPages ? page : null;
}
