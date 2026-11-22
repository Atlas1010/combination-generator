import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import {
  CombinationPage,
  GenerationSession,
  NextResponseDto,
  NextResult,
  PageRequest,
  PageResponseDto,
  StartResponseDto,
  toGetAllRequest,
  toNextResult,
  toPage,
  toSession,
} from './combinations.models';

// Thin HTTP adapter for the combinations server API.
@Injectable({ providedIn: 'root' })
export class CombinationsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/combinations';

  // Starts a new generation session for n values.
  start(n: number): Observable<GenerationSession> {
    return this.http.post<StartResponseDto>(`${this.baseUrl}/start`, { n }).pipe(map(toSession));
  }

  // Fetches the next combination of the session.
  next(sessionId: string): Observable<NextResult> {
    return this.http
      .post<NextResponseDto>(`${this.baseUrl}/${encodeURIComponent(sessionId)}/next`, null)
      .pipe(map(toNextResult));
  }

  // Fetches one page of combinations.
  getPage(sessionId: string, request: PageRequest): Observable<CombinationPage> {
    return this.http
      .post<PageResponseDto>(`${this.baseUrl}/${encodeURIComponent(sessionId)}/all`, toGetAllRequest(request))
      .pipe(map(toPage));
  }
}
