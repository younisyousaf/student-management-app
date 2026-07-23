import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.html',
  styleUrl: './shell.scss'
})
export class Shell {

  private authService = inject(AuthService);

  navLinks = [
    { path: '/students', label: 'Students' },
    { path: '/courses', label: 'Courses' },
    { path: '/enrollments', label: 'Enrollments' },
    { path: '/fees', label: 'Fees' },
    { path: '/attendance', label: 'Attendance' }
  ];

  logout(): void {
    this.authService.logout();
  }
}
