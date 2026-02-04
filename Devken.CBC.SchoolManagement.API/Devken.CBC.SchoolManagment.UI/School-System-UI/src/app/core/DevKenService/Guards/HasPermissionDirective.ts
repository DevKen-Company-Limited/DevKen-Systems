import {
    Directive,
    Input,
    TemplateRef,
    ViewContainerRef,
    OnDestroy
} from '@angular/core';
import { AuthService } from 'app/core/auth/auth.service';
import { Subject, combineLatest, takeUntil } from 'rxjs';

@Directive({
    selector: '[hasPermission]',
    standalone: true
})
export class HasPermissionDirective implements OnDestroy {

    private permissions: string[] = [];
    private requireAll = true;
    private hasView = false;

    private readonly _destroy$ = new Subject<void>();

    constructor(
        private templateRef: TemplateRef<any>,
        private viewContainer: ViewContainerRef,
        private authService: AuthService
    ) {
        // React to login, logout, and permissions updates
        combineLatest([
            this.authService['_authenticated'], // BehaviorSubject<boolean>
            this.authService.permissions$ || new Subject<string[]>() // optional observable if available
        ])
        .pipe(takeUntil(this._destroy$))
        .subscribe(() => this.updateView());
    }

    // ---------------------------------------------------------------------
    // REQUIRED PERMISSIONS
    // ---------------------------------------------------------------------
    @Input()
    set hasPermission(value: string | string[]) {
        this.permissions = Array.isArray(value) ? value : [value];
        this.updateView();
    }

    // ---------------------------------------------------------------------
    // REQUIRE ALL OR ANY
    // ---------------------------------------------------------------------
    @Input()
    set hasPermissionRequireAll(value: boolean) {
        this.requireAll = value;
        this.updateView();
    }

    // ---------------------------------------------------------------------
    // CORE LOGIC
    // ---------------------------------------------------------------------
    private updateView(): void {
        // Not authenticated → render nothing
        if (!this.authService['_authenticated'].value) {
            this.clear();
            return;
        }

        const userPermissions = this.authService.getUserPermissions() ?? [];

        if (!userPermissions.length) {
            this.clear();
            return;
        }

        const allowed = this.checkPermissions(userPermissions);

        if (allowed && !this.hasView) {
            this.viewContainer.createEmbeddedView(this.templateRef);
            this.hasView = true;
        } else if (!allowed && this.hasView) {
            this.clear();
        }
    }

    private checkPermissions(userPermissions: string[]): boolean {
        if (!this.permissions.length) {
            return false;
        }

        return this.requireAll
            ? this.permissions.every(p => userPermissions.includes(p))
            : this.permissions.some(p => userPermissions.includes(p));
    }

    private clear(): void {
        this.viewContainer.clear();
        this.hasView = false;
    }

    // ---------------------------------------------------------------------
    // CLEANUP
    // ---------------------------------------------------------------------
    ngOnDestroy(): void {
        this._destroy$.next();
        this._destroy$.complete();
    }
}
