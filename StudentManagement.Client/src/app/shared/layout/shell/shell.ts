import {
  Component,
  inject,
  signal
} from '@angular/core';
import {
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';
import {
  AuthService
} from '../../../core/services/auth.service';
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss'
})
export class Shell {
  private readonly authService =
    inject(AuthService);
  readonly isMobileNavOpen =
    signal(false);
  readonly navLinks = [
    {
      path: '/students',
      label: 'Students'
    },
    {
      path: '/courses',
      label: 'Courses'
    },
    {
      path: '/enrollments',
      label: 'Enrollments'
    },
    {
      path: '/fees',
      label: 'Fees'
    },
    {
      path: '/attendance',
      label: 'Attendance'
    },
    {
      path: '/copilot',
      label: 'AI Copilot'
    }
  ];
  toggleMobileNav(): void {
    this.isMobileNavOpen.update(
      isOpen => !isOpen
    );
  }
  closeMobileNav(): void {
    this.isMobileNavOpen.set(
      false
    );
  }
  logout(): void {
    this.authService.logout();
  }
}
