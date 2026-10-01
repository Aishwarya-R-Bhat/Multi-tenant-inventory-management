import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { Permissions } from '../core/permissions';
import { HasPermissionDirective } from '../shared/has-permission.directive';

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    HasPermissionDirective,
  ],
  styles: `
    .spacer {
      flex: 1;
    }
    nav {
      display: flex;
      gap: 4px;
      margin-left: 24px;
    }
    .who {
      margin-right: 12px;
      font-size: 14px;
    }
    main {
      padding: 24px;
      max-width: 1100px;
      margin: 0 auto;
    }
  `,
  template: `
    <mat-toolbar>
      <span>Inventory Platform</span>
      <nav>
        <a mat-button routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
        <a *hasPermission="permissions.UsersRead" mat-button routerLink="/admin/users" routerLinkActive="active">
          Users
        </a>
      </nav>
      <span class="spacer"></span>
      @if (auth.user(); as user) {
        <span class="who">{{ user.name }} · {{ user.tenantSlug }}</span>
      }
      <button mat-stroked-button type="button" (click)="logout()">Sign out</button>
    </mat-toolbar>
    <main><router-outlet /></main>
  `,
})
export class Shell {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly permissions = Permissions;

  protected logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
