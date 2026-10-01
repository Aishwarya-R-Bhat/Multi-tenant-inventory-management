import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Welcome, {{ auth.user()?.name }}</h1>
    <p>You are signed in to {{ auth.user()?.tenantSlug }}. Catalog, inventory and orders will appear here as they are built.</p>
  `,
})
export class Dashboard {
  protected readonly auth = inject(AuthService);
}
