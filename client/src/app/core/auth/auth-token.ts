import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { readTabItem, writeTabItem } from '../storage/tab-storage';

interface GuestTokenDto {
  accessToken: string;
  expiresAt: string;
}

interface CachedToken {
  value: string;
  expiresAt: number;
}

const EXPIRY_MARGIN_MS = 60_000;

const STORAGE_KEY = 'combinator.auth-token';

// Obtains and caches the API access token.
@Injectable({ providedIn: 'root' })
export class AuthToken {
  private readonly http = new HttpClient(inject(HttpBackend));
  private cached: CachedToken | null = restoreToken();
  private pending: Promise<string> | null = null;

  // Returns a valid token, requesting a new one when needed.
  get(): Promise<string> {
    if (this.cached && this.cached.expiresAt - EXPIRY_MARGIN_MS > Date.now()) {
      return Promise.resolve(this.cached.value);
    }

    this.pending ??= this.fetch().finally(() => (this.pending = null));
    return this.pending;
  }

  invalidate(): void {
    this.store(null);
  }

  private async fetch(): Promise<string> {
    const dto = await firstValueFrom(this.http.post<GuestTokenDto>('/api/auth/guest-token', null));
    this.store({ value: dto.accessToken, expiresAt: Date.parse(dto.expiresAt) });
    return dto.accessToken;
  }

  private store(token: CachedToken | null): void {
    this.cached = token;
    writeTabItem(STORAGE_KEY, token);
  }
}

function restoreToken(): CachedToken | null {
  const stored = readTabItem(STORAGE_KEY) as Partial<CachedToken> | null;
  return typeof stored?.value === 'string' && typeof stored.expiresAt === 'number'
    ? { value: stored.value, expiresAt: stored.expiresAt }
    : null;
}
