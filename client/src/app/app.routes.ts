import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadChildren: () => import('./features/combinations/combinations.routes').then((m) => m.COMBINATIONS_ROUTES) },
  { path: '**', redirectTo: '' },
];
