import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, shareReplay, catchError } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { of } from 'rxjs';

export interface EnumOption {
  id: string; // lowercase string
  name: string; // display name
  value: number; // numeric enum value
  description?: string;
  cssClass?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EnumService {
  private baseUrl = `${inject(API_BASE_URL)}/api/Enums`;
  private cache = new Map<string, Observable<EnumOption[]>>();

  constructor(private http: HttpClient) {}

  getBillingCycles(): Observable<EnumOption[]> {
    return this.getCached('billing-cycles');
  }

  getSubscriptionStatuses(): Observable<EnumOption[]> {
    return this.getCached('subscription-statuses');
  }

  getSubscriptionPlans(): Observable<EnumOption[]> {
    return this.getCached('subscription-plans');
  }

  private getCached(endpoint: string): Observable<EnumOption[]> {
    if (!this.cache.has(endpoint)) {
      const obs$ = this.http
        .get<any>(`${this.baseUrl}/${endpoint}`)
        .pipe(
          map((res: any) =>
            res.success && Array.isArray(res.data)
              ? res.data.map((item: any) => ({
                  id: String(item.id).toLowerCase(),
                  name: String(item.name),
                  value: Number(item.value), // Ensure numeric value
                  description: item.description || '',
                  cssClass: item.cssClass || ''
                }))
              : []
          ),
          catchError((error) => {
            console.error(`Error loading ${endpoint}:`, error);
            return of([]);
          }),
          shareReplay(1)
        );
      this.cache.set(endpoint, obs$);
    }
    return this.cache.get(endpoint)!;
  }

  // ================== Helpers ==================

  /**
   * Get display name by numeric enum value
   */
  getDisplayNameByValue(options: EnumOption[], value: number): string {
    if (!Array.isArray(options)) return value.toString();
    
    const option = options.find((o) => Number(o.value) === Number(value));
    return option?.name ?? value.toString();
  }

  /**
   * Get CSS class by numeric enum value
   */
  getCssClassByValue(options: EnumOption[], value: number): string {
    if (!Array.isArray(options)) return 'bg-secondary';
    
    const option = options.find((o) => Number(o.value) === Number(value));
    return option?.cssClass || 'bg-secondary';
  }

  /**
   * Clear all cached data
   */
  clearCache(): void {
    this.cache.clear();
  }

  /**
   * Clear specific cache entry
   */
  clearCacheEntry(endpoint: string): void {
    this.cache.delete(endpoint);
  }
}