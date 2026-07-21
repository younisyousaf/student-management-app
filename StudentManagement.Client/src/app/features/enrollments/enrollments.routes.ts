import { Routes } from '@angular/router';

export const ENROLLMENTS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./enrollment-list/enrollment-list').then(m => m.EnrollmentList) },
  { path: 'new', loadComponent: () => import('./enrollment-form/enrollment-form').then(m => m.EnrollmentForm) }
];
