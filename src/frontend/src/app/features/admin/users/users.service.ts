import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  list(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${environment.apiBaseUrl}/users`);
  }
}
