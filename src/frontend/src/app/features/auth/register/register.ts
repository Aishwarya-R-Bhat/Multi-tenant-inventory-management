import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ApiError } from '../../../core/http/api-error';

@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  styleUrl: '../auth-page.scss',
  template: `
    <mat-card>
      <mat-card-header><mat-card-title>Create your company</mat-card-title></mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <mat-form-field>
            <mat-label>Company name</mat-label>
            <input matInput formControlName="companyName" autocomplete="organization" />
          </mat-form-field>
          <mat-form-field>
            <mat-label>Company code</mat-label>
            <input matInput formControlName="slug" />
            <mat-hint>Lowercase letters, digits and hyphens. Used when signing in.</mat-hint>
            @if (form.controls.slug.hasError('pattern')) {
              <mat-error>Use only lowercase letters, digits and hyphens.</mat-error>
            }
          </mat-form-field>
          <mat-form-field>
            <mat-label>Your name</mat-label>
            <input matInput formControlName="adminFullName" autocomplete="name" />
          </mat-form-field>
          <mat-form-field>
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="adminEmail" autocomplete="username" />
          </mat-form-field>
          <mat-form-field>
            <mat-label>Password</mat-label>
            <input matInput type="password" formControlName="password" autocomplete="new-password" />
            <mat-hint>At least 8 characters</mat-hint>
          </mat-form-field>

          @if (errors().length) {
            <ul class="error" role="alert">
              @for (e of errors(); track e) {
                <li>{{ e }}</li>
              }
            </ul>
          }

          <button mat-flat-button type="submit" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Creating…' : 'Create company' }}
          </button>
        </form>
        <p class="switch">Already registered? <a routerLink="/login">Sign in</a></p>
      </mat-card-content>
    </mat-card>
  `,
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly errors = signal<string[]>([]);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],
    slug: ['', [Validators.required, Validators.maxLength(50), Validators.pattern(/^[a-z0-9-]+$/)]],
    adminFullName: ['', Validators.required],
    adminEmail: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.errors.set([]);

    this.auth.registerTenant(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigateByUrl('/'),
      error: (e: ApiError) => {
        const fieldMessages = Object.values(e.fieldErrors).flat();
        this.errors.set(fieldMessages.length ? fieldMessages : [e.message]);
        this.loading.set(false);
      },
    });
  }
}
