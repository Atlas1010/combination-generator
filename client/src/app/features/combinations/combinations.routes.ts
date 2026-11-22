import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';

import { CombinationsStore } from './state/combinations-store';

// Redirects to the setup page when no session exists.
const requireSession: CanActivateFn = () =>
  inject(CombinationsStore).hasSession() || inject(Router).createUrlTree(['/']);

export const COMBINATIONS_ROUTES: Routes = [
  {
    path: '',
    title: 'מחולל הקומבינציות',
    loadComponent: () => import('./pages/setup-page/setup-page').then((m) => m.SetupPage),
  },
  {
    path: 'generator',
    title: 'מחולל הקומבינציות',
    canActivate: [requireSession],
    loadComponent: () => import('./pages/generator-page/generator-page').then((m) => m.GeneratorPage),
  },
];
