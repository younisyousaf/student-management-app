import { Routes } from '@angular/router';

export const ATTENDANCE_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./attendance-list/attendance-list').then(m => m.AttendanceList) },
  { path: 'mark', loadComponent: () => import('./attendance-form/attendance-form').then(m => m.AttendanceForm) },
  { path: ':id/edit', loadComponent: () => import('./attendance-form/attendance-form').then(m => m.AttendanceForm) }
];
