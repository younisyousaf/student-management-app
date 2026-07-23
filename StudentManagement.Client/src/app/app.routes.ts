import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { loginGuard } from './core/guards/login.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [loginGuard],
    loadComponent: () => import('./features/auth/login/login').then(m => m.Login)
  },
  {
    path: 'register',
    canActivate: [loginGuard],
    loadComponent: () => import('./features/auth/register/register').then(m => m.Register)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./shared/layout/shell/shell').then(m => m.Shell),
    children: [
      {
        path: '',
        redirectTo: 'students',
        pathMatch: 'full'
      },
      {
        path: 'students',
        loadChildren: () => import('./features/students/students.routes').then(m => m.STUDENTS_ROUTES)
      },
      {
        path: 'courses',
        loadChildren: () => import('./features/courses/courses.routes').then(m => m.COURSES_ROUTES)
      },
      {
        path: 'enrollments',
        loadChildren: () => import('./features/enrollments/enrollments.routes').then(m => m.ENROLLMENTS_ROUTES)
      },
      {
        path: 'fees',
        loadChildren: () => import('./features/fees/fees.routes').then(m => m.FEES_ROUTES)
      },
      {
        path: 'attendance',
        loadChildren: () => import('./features/attendance/attendance.routes').then(m => m.ATTENDANCE_ROUTES)
      }
    ]
  },

];
