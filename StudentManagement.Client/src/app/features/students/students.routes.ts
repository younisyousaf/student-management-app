import { Routes } from '@angular/router';

export const STUDENTS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./student-list/student-list').then(m => m.StudentList) },
  { path: 'new', loadComponent: () => import('./student-form/student-form').then(m => m.StudentForm) },
  { path: ':id/edit', loadComponent: () => import('./student-form/student-form').then(m => m.StudentForm) }
];
