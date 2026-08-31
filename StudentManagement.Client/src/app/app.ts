import { Component, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { ToastContainer } from './shared/components/toast-container/toast-container';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainer],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('SmartCampus');

  constructor(private router: Router) {
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(event => {
      const url = (event as NavigationEnd).urlAfterRedirects;
      if (!url.startsWith('/login') && !url.startsWith('/register')) sessionStorage.setItem('lastRoute', url);
    });
  }
}
