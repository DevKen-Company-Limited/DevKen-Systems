// app/core/user/user.service.ts
import { HttpClient }                        from '@angular/common/http';
import { inject, Injectable }                from '@angular/core';
import { User }                              from 'app/core/user/user.types';
import { map, Observable, ReplaySubject, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UserService {

    private _httpClient = inject(HttpClient);
    private _user: ReplaySubject<User> = new ReplaySubject<User>(1);

    // ── Accessors ──────────────────────────────────────────────────────────────

    /**
     * Setter — accepts a full User, a partial AuthUser-shaped object, or null.
     *
     * AuthService calls this with:
     *   - an AuthUser object on login / session restore
     *   - null on sign-out
     *
     * When value is null we simply do not emit, which leaves the ReplaySubject
     * holding whatever its last value was.  Downstream consumers that need to
     * react to sign-out should subscribe to AuthService.authenticated$ instead.
     *
     * When value is an AuthUser (id, name, email, fullName, roles, permissions)
     * we map it onto the Fuse User shape so the rest of the app keeps working.
     */
    set user(value: User | Partial<User> | null) {
        if (value == null) return;   // sign-out — nothing to emit

        // Normalise: AuthUser uses `name` / `fullName`; Fuse User uses `name`.
        // We accept any object that at least has an id + email and bridge the rest.
        const normalised: User = {
            id     : (value as any).id    ?? '',
            name   : (value as any).name  ?? (value as any).fullName ?? '',
            email  : (value as any).email ?? '',
            avatar : (value as any).avatar         ?? (value as any).profileImageUrl ?? null,
            status : (value as any).status         ?? 'online',
            // Carry through anything else the caller supplied
            ...(value as any),
        };

        this._user.next(normalised);
    }

    get user$(): Observable<User> {
        return this._user.asObservable();
    }
    

    // ── Public methods ─────────────────────────────────────────────────────────

    /**
     * Get the current signed-in user data from the Fuse mock API.
     * Not used when the app is backed by the real Devken CBC API —
     * AuthService.checkAuthOnStartup() populates the user instead.
     */
    get(): Observable<User> {
        return this._httpClient.get<User>('api/common/user').pipe(
            tap(user => this._user.next(user))
        );
    }

    /**
     * Update the user via the Fuse mock API.
     */
    update(user: User): Observable<any> {
        return this._httpClient.patch<User>('api/common/user', { user }).pipe(
            map(response => this._user.next(response))
        );
    }
}