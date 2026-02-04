import { HttpEvent, HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { FuseMockApiService } from '@fuse/lib/mock-api/mock-api.service';
import { Observable, throwError } from 'rxjs';
import { switchMap, delay } from 'rxjs/operators';

export const mockApiInterceptor = (
    req: HttpRequest<unknown>,
    next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
    const fuseMockApiService = inject(FuseMockApiService);

    // Only intercept requests starting with 'api/'
    if (!req.url.startsWith('api/')) {
        return next(req);
    }

    // Find matching mock handler
    const { handler, urlParams, isPassThrough } = fuseMockApiService.findHandler(
        req.method.toLowerCase(),
        req.url
    );

    // Pass-through for non-mock requests
    if (isPassThrough || !handler) {
        return next(req);
    }

    // Set request and URL params on the handler
    handler.request = req;
    handler.urlParams = urlParams;

    // Use the response Observable from the handler
    return handler.response.pipe(
        delay(handler.delay || 0),
        switchMap((res) => {
            // res must be [status, body]
            if (!Array.isArray(res) || res.length !== 2) {
                return throwError(() => new Error('Invalid response format from mock handler'));
            }

            const [status, body] = res;

            if (status >= 200 && status < 300) {
                return new Observable<HttpEvent<unknown>>((observer) => {
                    observer.next(new HttpResponse({ status, body, statusText: 'OK' }));
                    observer.complete();
                });
            }

            return throwError(() => ({ error: body, status }));
        })
    );
};
