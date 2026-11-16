import { Routes } from '@angular/router';
import { authGuard } from './auth';
import { Home } from './home';
import { LoginPage } from './login-page';
import { ProtectedPage } from './protected-page';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'login', component: LoginPage },

  // Guarded routes. Admin is guarded only by "logged in" on purpose:
  // whether you may see it is decided by the API (403).
  { path: 'secret', component: ProtectedPage, canActivate: [authGuard], data: { title: 'Secret', url: '/api/secret' } },
  { path: 'admin', component: ProtectedPage, canActivate: [authGuard], data: { title: 'Admin', url: '/api/admin' } },
];
