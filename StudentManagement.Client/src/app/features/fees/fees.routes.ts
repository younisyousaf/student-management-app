import { Routes } from '@angular/router';

export const FEES_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./fee-list/fee-list').then(m => m.FeeList) },
  { path: 'pay', loadComponent: () => import('./fee-payment/fee-payment').then(m => m.FeePayment) }
];
