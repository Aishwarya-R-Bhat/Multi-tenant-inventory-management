import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/guards';
import { Permissions } from './core/permissions';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login').then((m) => m.Login) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register').then((m) => m.Register) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'admin/users',
        canActivate: [permissionGuard(Permissions.UsersRead)],
        loadComponent: () => import('./features/admin/users/users-list').then((m) => m.UsersList),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
