import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html'
})
export class Register {
  username = signal('');
  email = signal('');
  password = signal('');
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  success = signal(false);

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.authService.register({
      username: this.username(),
      email: this.email(),
      password: this.password()
    }).subscribe({
      next: () => {
        this.success.set(true);
        this.isLoading.set(false);
        setTimeout(() => this.router.navigate(['/login']), 1200);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Registration failed.');
        this.isLoading.set(false);
      }
    });
  }
}
