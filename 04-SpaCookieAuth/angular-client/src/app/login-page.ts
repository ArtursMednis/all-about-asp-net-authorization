import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, User, safeReturnUrl } from './auth';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule],
  template: `
    <h1>Login</h1>
    <form (ngSubmit)="submit()">
      <input name="userName" placeholder="User name" [(ngModel)]="userName" />
      <input name="password" placeholder="Password" type="password" [(ngModel)]="password" />
      <button type="submit">Login</button>
    </form>
    @if (error()) {
      <p style="color: red">{{ error() }}</p>
    }
    <p>Try <b>user</b> / <b>user</b> or <b>admin</b> / <b>admin</b>.</p>
  `,
})
export class LoginPage {
  // From the query string: /login?returnUrl=/secret
  readonly returnUrl = input<string>();

  protected userName = '';
  protected password = '';
  protected error = signal('');

  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private router = inject(Router);

  submit() {
    // On success the response sets the cookie - we only get the user's name back.
    this.http.post<User>('/api/account/login', { userName: this.userName, password: this.password }).subscribe({
      next: user => {
        this.auth.user.set(user);
        this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
      },
      error: (error: HttpErrorResponse) => this.error.set(error.error?.error),
    });
  }
}
