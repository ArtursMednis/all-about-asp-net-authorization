import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './auth';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  template: `
    <nav>
      <a routerLink="/">Home</a> | <a routerLink="/secret">Secret</a> | <a routerLink="/admin">Admin</a> |
      @if (auth.user(); as user) {
        Logged in as <b>{{ user.name }}</b> <button (click)="logout()">Logout</button>
      } @else {
        <a routerLink="/login">Login</a>
      }
    </nav>
    <hr />

    <router-outlet />
  `,
})
export class App {
  protected auth = inject(AuthService);
  private http = inject(HttpClient);
  private router = inject(Router);

  logout() {
    this.http.post('/api/account/logout', null).subscribe(() => {
      this.auth.user.set(null);
      this.router.navigateByUrl('/');
    });
  }
}
