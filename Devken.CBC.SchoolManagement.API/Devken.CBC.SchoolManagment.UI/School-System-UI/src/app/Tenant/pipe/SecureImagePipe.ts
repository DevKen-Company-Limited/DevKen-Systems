import { Pipe, PipeTransform, OnDestroy, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import {
  Observable, Subject, of, BehaviorSubject
} from 'rxjs';
import {
  switchMap, takeUntil, map, catchError, distinctUntilChanged
} from 'rxjs/operators';

/**
 * SecureImagePipe
 *
 * Fetches an image URL through Angular's HttpClient so that any
 * HTTP interceptors (e.g. the auth-token interceptor) are applied.
 * The server response is converted to an object URL and sanitized
 * for safe binding to [src].
 *
 * Usage in template:
 *   <img [src]="school.logoUrl | secureImage | async" ... />
 *
 * Returns null while loading or on error, allowing the template to
 * show a fallback icon instead.
 */
@Pipe({
  name: 'secureImage',
  standalone: true,
  pure: false   // must be impure so it reacts to async emissions
})
export class SecureImagePipe implements PipeTransform, OnDestroy {
  private http        = inject(HttpClient);
  private sanitizer   = inject(DomSanitizer);

  // Track the current object URL so we can revoke it on change
  private _currentObjectUrl: string | null = null;
  private _destroy$   = new Subject<void>();
  private _urlInput$  = new BehaviorSubject<string | null | undefined>(null);
  private _result$!:  Observable<SafeUrl | null>;

  constructor() {
    this._result$ = this._urlInput$.pipe(
      distinctUntilChanged(),
      switchMap(url => {
        // Revoke the previous blob URL to avoid memory leaks
        this._revokeCurrentUrl();

        if (!url) return of(null);

        return this.http.get(url, { responseType: 'blob' }).pipe(
          map(blob => {
            const objectUrl = URL.createObjectURL(blob);
            this._currentObjectUrl = objectUrl;
            return this.sanitizer.bypassSecurityTrustUrl(objectUrl);
          }),
          catchError(() => of(null))
        );
      }),
      takeUntil(this._destroy$)
    );
  }

  transform(url: string | null | undefined): Observable<SafeUrl | null> {
    this._urlInput$.next(url);
    return this._result$;
  }

  ngOnDestroy(): void {
    this._revokeCurrentUrl();
    this._destroy$.next();
    this._destroy$.complete();
  }

  private _revokeCurrentUrl(): void {
    if (this._currentObjectUrl) {
      URL.revokeObjectURL(this._currentObjectUrl);
      this._currentObjectUrl = null;
    }
  }
}