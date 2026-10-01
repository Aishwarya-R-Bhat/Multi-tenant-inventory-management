import { Directive, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { AuthService } from '../core/auth/auth.service';

/** Usage: <button *hasPermission="'Users.Manage'">. Hides UI only, the API checks the permission again. */
@Directive({ selector: '[hasPermission]' })
export class HasPermissionDirective {
  readonly hasPermission = input.required<string>();

  private readonly auth = inject(AuthService);
  private readonly template = inject(TemplateRef);
  private readonly container = inject(ViewContainerRef);

  constructor() {
    effect(() => {
      this.container.clear();
      if (this.auth.hasPermission(this.hasPermission())) {
        this.container.createEmbeddedView(this.template);
      }
    });
  }
}
