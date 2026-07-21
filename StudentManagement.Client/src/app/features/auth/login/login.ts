import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) { }

  onSubmit(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);

    this.authService.login({ username: this.username(), password: this.password() })
      .subscribe({
        next: () => {
          this.authService.setAuthenticated(true);

          const returnUrl = this.route.snapshot.queryParams['returnUrl'];
          const lastRoute = sessionStorage.getItem('lastRoute');

          this.router.navigateByUrl(returnUrl || lastRoute || '/students');
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message ?? 'Invalid username or password.');
          this.isLoading.set(false);
        }
      });
  }
}
