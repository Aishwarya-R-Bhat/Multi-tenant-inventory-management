import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { ApiError } from '../../../core/http/api-error';
import { UserDto, UsersService } from './users.service';

@Component({
  selector: 'app-users-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTableModule],
  styles: `
    table {
      width: 100%;
    }
    .error {
      color: var(--mat-sys-error);
    }
  `,
  template: `
    <h1>Users</h1>
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }
    <table mat-table [dataSource]="users()">
      <ng-container matColumnDef="fullName">
        <th mat-header-cell *matHeaderCellDef>Name</th>
        <td mat-cell *matCellDef="let u">{{ u.fullName }}</td>
      </ng-container>
      <ng-container matColumnDef="email">
        <th mat-header-cell *matHeaderCellDef>Email</th>
        <td mat-cell *matCellDef="let u">{{ u.email }}</td>
      </ng-container>
      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef>Status</th>
        <td mat-cell *matCellDef="let u">{{ u.isActive ? 'Active' : 'Disabled' }}</td>
      </ng-container>
      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns"></tr>
    </table>
  `,
})
export class UsersList {
  protected readonly columns = ['fullName', 'email', 'isActive'];
  protected readonly users = signal<UserDto[]>([]);
  protected readonly error = signal<string | null>(null);

  constructor() {
    inject(UsersService)
      .list()
      .subscribe({
        next: (users) => this.users.set(users),
        error: (e: ApiError) => this.error.set(e.message),
      });
  }
}
