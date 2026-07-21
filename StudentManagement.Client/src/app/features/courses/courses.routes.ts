import { Routes } from '@angular/router';

export const COURSES_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./course-list/course-list').then(m => m.CourseList) },
  { path: 'new', loadComponent: () => import('./course-form/course-form').then(m => m.CourseForm) },
  { path: ':id/edit', loadComponent: () => import('./course-form/course-form').then(m => m.CourseForm) }
];
