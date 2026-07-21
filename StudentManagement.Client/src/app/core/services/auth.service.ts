import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment.development';
import { LoginRequest, RegisterRequest, MeResponse } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}/Auth`;
  private lastRoute = '/students';

  private authenticatedSignal = signal<boolean>(false);
  readonly isAuthenticated = this.authenticatedSignal.asReadonly();

  constructor(private http: HttpClient, private router: Router) { }

  login(credentials: LoginRequest) {
    return this.http.post<{ message: string }>(`${this.baseUrl}/login`, credentials, { withCredentials: true });
  }

  register(payload: RegisterRequest) {
    return this.http.post<{ message: string }>(`${this.baseUrl}/register`, payload, { withCredentials: true });
  }

  logout(): void {
    this.http.post(`${this.baseUrl}/logout`, {}, { withCredentials: true }).subscribe({
      complete: () => {
        this.authenticatedSignal.set(false);
        this.router.navigate(['/login']);
      }
    });
  }

  checkSession() {
    return this.http.get<MeResponse>(`${this.baseUrl}/me`, { withCredentials: true });
  }

  setAuthenticated(value: boolean): void {
    this.authenticatedSignal.set(value);
  }

  setLastRoute(route: string): void {
    this.lastRoute = route;
  }

  getLastRoute(): string {
    return this.lastRoute;
  }
}
