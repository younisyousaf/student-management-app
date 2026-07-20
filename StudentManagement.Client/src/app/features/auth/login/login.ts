import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html'
})
export class Login {
  username = signal('');
  password = signal('');
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.authService.login({ username: this.username(), password: this.password() })
      .subscribe({
        next: () => {
          this.authService.setAuthenticated(true);
          this.router.navigate(['/students']);
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message ?? 'Invalid username or password.');
          this.isLoading.set(false);
        }
      });
  }
}
