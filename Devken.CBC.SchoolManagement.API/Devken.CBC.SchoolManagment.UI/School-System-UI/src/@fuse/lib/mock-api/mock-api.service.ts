import { Injectable, Inject } from '@angular/core';
import { FuseMockApiHandler } from '@fuse/lib/mock-api/mock-api.request-handler';
import { FuseMockApiMethods } from '@fuse/lib/mock-api/mock-api.types';
import { compact, fromPairs } from 'lodash-es';
import { API_BASE_URL } from 'app/app.config';

@Injectable({ providedIn: 'root' })
export class FuseMockApiService {
    private _handlers: Record<FuseMockApiMethods, Map<string, FuseMockApiHandler>> = {
        get: new Map<string, FuseMockApiHandler>(),
        post: new Map<string, FuseMockApiHandler>(),
        patch: new Map<string, FuseMockApiHandler>(),
        delete: new Map<string, FuseMockApiHandler>(),
        put: new Map<string, FuseMockApiHandler>(),
        head: new Map<string, FuseMockApiHandler>(),
        jsonp: new Map<string, FuseMockApiHandler>(),
        options: new Map<string, FuseMockApiHandler>(),
    };

    // URLs that should bypass the mock API
    private _passThroughUrls: Set<string> = new Set();

    constructor(@Inject(API_BASE_URL) private _apiBaseUrl: string) {}

    // -----------------------------------------------------------------------------------------------------
    // Public methods for pass-through
    // -----------------------------------------------------------------------------------------------------
    addPassThroughUrl(url: string): void {
        const fullUrl = url.startsWith('http') ? url : `${this._apiBaseUrl}/${url}`;
        this._passThroughUrls.add(fullUrl);
    }

    removePassThroughUrl(url: string): void {
        const fullUrl = url.startsWith('http') ? url : `${this._apiBaseUrl}/${url}`;
        this._passThroughUrls.delete(fullUrl);
    }

    isPassThroughUrl(url: string): boolean {
        const fullUrl = url.startsWith('http') ? url : `${this._apiBaseUrl}/${url}`;
        return this._passThroughUrls.has(fullUrl);
    }

    // -----------------------------------------------------------------------------------------------------
    // Find a handler for a request
    // -----------------------------------------------------------------------------------------------------
    findHandler(
        method: string,
        url: string
    ): {
        handler: FuseMockApiHandler | undefined;
        urlParams: Record<string, string>;
        isPassThrough: boolean;
    } {
        const fullUrl = url.startsWith('http') ? url : `${this._apiBaseUrl}/${url}`;
        const isPassThrough = this.isPassThroughUrl(fullUrl);

        const matching: {
            handler: FuseMockApiHandler | undefined;
            urlParams: Record<string, string>;
            isPassThrough: boolean;
        } = {
            handler: undefined,
            urlParams: {},
            isPassThrough,
        };

        if (isPassThrough) return matching;

        const urlParts = fullUrl.split('/').filter(Boolean);
        const handlers = this._handlers[method.toLowerCase() as FuseMockApiMethods];

        handlers.forEach((handler, handlerUrl) => {
            if (matching.handler) return; // already found a match

            const handlerFullUrl = handlerUrl.startsWith('http') ? handlerUrl : `${this._apiBaseUrl}/${handlerUrl}`;
            const handlerParts = handlerFullUrl.split('/').filter(Boolean);

            if (urlParts.length !== handlerParts.length) return;

            const matches = handlerParts.every((part, i) => part === urlParts[i] || part.startsWith(':'));
            if (!matches) return;

            matching.handler = handler;
            matching.urlParams = fromPairs(
                compact(
                    handlerParts.map((part, i) =>
                        part.startsWith(':') ? [part.substring(1), urlParts[i]] : undefined
                    )
                )
            );
        });

        return matching;
    }

    // -----------------------------------------------------------------------------------------------------
    // Public helper methods to register handlers
    // -----------------------------------------------------------------------------------------------------
    onGet(url: string, delay?: number) { return this._registerHandler('get', url, delay); }
    onPost(url: string, delay?: number) { return this._registerHandler('post', url, delay); }
    onPatch(url: string, delay?: number) { return this._registerHandler('patch', url, delay); }
    onDelete(url: string, delay?: number) { return this._registerHandler('delete', url, delay); }
    onPut(url: string, delay?: number) { return this._registerHandler('put', url, delay); }
    onHead(url: string, delay?: number) { return this._registerHandler('head', url, delay); }
    onJsonp(url: string, delay?: number) { return this._registerHandler('jsonp', url, delay); }
    onOptions(url: string, delay?: number) { return this._registerHandler('options', url, delay); }

    // -----------------------------------------------------------------------------------------------------
    // Private method to register a handler
    // -----------------------------------------------------------------------------------------------------
    private _registerHandler(method: FuseMockApiMethods, url: string, delay?: number): FuseMockApiHandler {
        const fullUrl = url.startsWith('http') ? url : `${this._apiBaseUrl}/${url}`;
        const handler = new FuseMockApiHandler(fullUrl, delay);
        this._handlers[method].set(fullUrl, handler);
        return handler;
    }
}
