import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./features/auth/login/login').then(m => m.Login) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register').then(m => m.Register) },
  // {
  //   path: 'students',
  //   canActivate: [authGuard],
  //   // loadChildren: () => import('./features/students/students.routes').then(m => m.STUDENTS_ROUTES)
  // }
];
