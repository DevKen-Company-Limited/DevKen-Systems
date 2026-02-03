import { HttpEvent, HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { FuseMockApiService } from '@fuse/lib/mock-api/mock-api.service';
import { Observable, delay, of, switchMap, throwError } from 'rxjs';

export const mockApiInterceptor = (
    req: HttpRequest<unknown>,
    next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
    const fuseMockApiService = inject(FuseMockApiService);

    // Try to get the request URL
    let requestUrl = req.url;

    // If the request URL doesn't start with 'api/', continue normally
    if (!requestUrl.startsWith('api/')) {
        return next(req);
    }

    // Find the matching handler
    const { handler, urlParams, isPassThrough } = fuseMockApiService.findHandler(
        req.method.toLowerCase(),
        requestUrl
    );

    // If this URL should pass through to real API, continue normally
    if (isPassThrough) {
        return next(req);
    }

    // If there is no matching handler, continue normally
    if (!handler) {
        return next(req);
    }

    // Set the intercepted request on the handler
    handler.request = req;

    // Set the URL params on the handler
    handler.urlParams = urlParams;

    // Subscribe to the reply function to get the response
    // return handler.reply$.pipe(
    //     delay(handler.delay),
    //     switchMap((response) => {
    //         // If the response is falsy, throw an error
    //         if (!response) {
    //             return throwError(() => new Error('Response is undefined'));
    //         }

    //         // Parse the response data
    //         const data = {
    //             status: response[0],
    //             body: response[1],
    //         };

    //         // If the status is 200, return success response
    //         if (data.status >= 200 && data.status < 300) {
    //             return of(
    //                 new HttpResponse({
    //                     body: data.body,
    //                     status: data.status,
    //                     statusText: 'OK',
    //                 })
    //             );
    //         }

            // If status is not 200, throw an error
    //         return throwError(() => ({
    //             error: data.body,
    //             status: data.status,
    //         }));
    //     })
    // );
};