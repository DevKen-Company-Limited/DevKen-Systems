import { Directive, Input, OnInit, TemplateRef, ViewContainerRef } from '@angular/core';
import { AuthService } from 'app/core/auth/auth.service';

@Directive({
    selector: '[hasPermission]',
    standalone: true,
})
export class HasPermissionDirective implements OnInit {
    private permissions: string[] = [];
    private requireAll = true;

    constructor(
        private templateRef: TemplateRef<any>,
        private viewContainer: ViewContainerRef,
        private authService: AuthService
    ) {}

    @Input()
    set hasPermission(val: string | string[]) {
        this.permissions = Array.isArray(val) ? val : [val];
        this.updateView();
    }

    @Input()
    set hasPermissionRequireAll(val: boolean) {
        this.requireAll = val;
        this.updateView();
    }

    ngOnInit(): void {
        this.updateView();
    }

    private updateView(): void {
        const userPermissions = this.authService.getUserPermissions();
        const hasPermission = this.checkPermissions(userPermissions);

        if (hasPermission) {
            this.viewContainer.createEmbeddedView(this.templateRef);
        } else {
            this.viewContainer.clear();
        }
    }

    private checkPermissions(userPermissions: string[]): boolean {
        if (this.permissions.length === 0) {
            return true;
        }

        if (this.requireAll) {
            return this.permissions.every((permission) => userPermissions.includes(permission));
        } else {
            return this.permissions.some((permission) => userPermissions.includes(permission));
        }
    }
}

// Usage examples:
// <button *hasPermission="'Student.Write'">Add Student</button>
// <button *hasPermission="['Student.Write', 'Student.Delete']; requireAll: true">Manage Students</button>
// <div *hasPermission="['Fee.Read', 'Payment.Read']; requireAll: false">Finance Section</div>