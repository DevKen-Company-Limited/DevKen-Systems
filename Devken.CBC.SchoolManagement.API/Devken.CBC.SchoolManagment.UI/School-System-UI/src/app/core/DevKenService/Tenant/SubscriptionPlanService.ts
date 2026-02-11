import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, shareReplay, catchError } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { of } from 'rxjs';

export interface SubscriptionPlanDto {
  planValue: number;
  id: string;
  planType: string;
  name: string;
  description: string;
  monthlyPrice: number;
  quarterlyPrice: number;
  yearlyPrice: number;
  currency: string;
  maxStudents: number;
  maxTeachers: number;
  maxStorageGB: number;
  features: string[];
  enabledFeatures: string[];
  displayOrder: number;
  isMostPopular: boolean;
  isActive: boolean;
  quarterlyDiscountPercent: number;
  yearlyDiscountPercent: number;
  quarterlyDiscountText: string;
  yearlyDiscountText: string;
}

@Injectable({
  providedIn: 'root'
})
export class SubscriptionPlanService {
  private baseUrl = `${inject(API_BASE_URL)}/api/subscription-plans`;
  private plansCache$?: Observable<SubscriptionPlanDto[]>;

  constructor(private http: HttpClient) {}

  /**
   * Get all subscription plans
   */
  getAllPlans(includeInactive: boolean = false): Observable<SubscriptionPlanDto[]> {
    if (!includeInactive && this.plansCache$) {
      return this.plansCache$;
    }

    const observable = this.http
      .get<any>(`${this.baseUrl}?includeInactive=${includeInactive}`)
      .pipe(
        map((response) => {
          if (response.success && response.data) {
            return response.data.map((plan: any) => ({
              planValue: plan.planValue || plan.id,
              id: plan.id,
              planType: plan.planType || plan.name.toLowerCase(),
              name: plan.name,
              description: plan.description,
              monthlyPrice: plan.monthlyPrice || 0,
              quarterlyPrice: plan.quarterlyPrice || 0,
              yearlyPrice: plan.yearlyPrice || 0,
              currency: plan.currency || 'KES',
              maxStudents: plan.maxStudents || 0,
              maxTeachers: plan.maxTeachers || 0,
              maxStorageGB: plan.maxStorageGB || 0,
              features: Array.isArray(plan.features) ? plan.features : [],
              enabledFeatures: Array.isArray(plan.enabledFeatures)
                ? plan.enabledFeatures
                : [],
              displayOrder: plan.displayOrder || 0,
              isMostPopular: plan.isMostPopular || false,
              isActive: plan.isActive !== false,
              quarterlyDiscountPercent: plan.quarterlyDiscountPercent || 0,
              yearlyDiscountPercent: plan.yearlyDiscountPercent || 0,
              quarterlyDiscountText: plan.quarterlyDiscountText || '',
              yearlyDiscountText: plan.yearlyDiscountText || ''
            } as SubscriptionPlanDto));
          }
          return [];
        }),
        catchError((error) => {
          console.error('Error loading subscription plans:', error);
          return of([]);
        }),
        shareReplay(1)
      );

    if (!includeInactive) {
      this.plansCache$ = observable;
    }

    return observable;
  }

  /**
   * Get plan by ID
   */
  getPlanById(planId: string): Observable<SubscriptionPlanDto | null> {
    return this.http.get<any>(`${this.baseUrl}/${planId}`).pipe(
      map((response) => {
        if (response.success && response.data) {
          const plan = response.data;
          return {
            planValue: plan.planValue || plan.id,
            id: plan.id,
            planType: plan.planType || plan.name.toLowerCase(),
            name: plan.name,
            description: plan.description,
            monthlyPrice: plan.monthlyPrice || 0,
            quarterlyPrice: plan.quarterlyPrice || 0,
            yearlyPrice: plan.yearlyPrice || 0,
            currency: plan.currency || 'KES',
            maxStudents: plan.maxStudents || 0,
            maxTeachers: plan.maxTeachers || 0,
            maxStorageGB: plan.maxStorageGB || 0,
            features: Array.isArray(plan.features) ? plan.features : [],
            enabledFeatures: Array.isArray(plan.enabledFeatures)
              ? plan.enabledFeatures
              : [],
            displayOrder: plan.displayOrder || 0,
            isMostPopular: plan.isMostPopular || false,
            isActive: plan.isActive !== false,
            quarterlyDiscountPercent: plan.quarterlyDiscountPercent || 0,
            yearlyDiscountPercent: plan.yearlyDiscountPercent || 0,
            quarterlyDiscountText: plan.quarterlyDiscountText || '',
            yearlyDiscountText: plan.yearlyDiscountText || ''
          } as SubscriptionPlanDto;
        }
        return null;
      }),
      catchError((error) => {
        console.error('Error loading plan:', error);
        return of(null);
      })
    );
  }

  /**
   * Get plan by type
   */
  getPlanByType(planType: string): Observable<SubscriptionPlanDto | null> {
    return this.http.get<any>(`${this.baseUrl}/type/${planType}`).pipe(
      map((response) => {
        if (response.success && response.data) {
          const plan = response.data;
          return {
            planValue: plan.planValue || plan.id,
            id: plan.id,
            planType: plan.planType || plan.name.toLowerCase(),
            name: plan.name,
            description: plan.description,
            monthlyPrice: plan.monthlyPrice || 0,
            quarterlyPrice: plan.quarterlyPrice || 0,
            yearlyPrice: plan.yearlyPrice || 0,
            currency: plan.currency || 'KES',
            maxStudents: plan.maxStudents || 0,
            maxTeachers: plan.maxTeachers || 0,
            maxStorageGB: plan.maxStorageGB || 0,
            features: Array.isArray(plan.features) ? plan.features : [],
            enabledFeatures: Array.isArray(plan.enabledFeatures)
              ? plan.enabledFeatures
              : [],
            displayOrder: plan.displayOrder || 0,
            isMostPopular: plan.isMostPopular || false,
            isActive: plan.isActive !== false,
            quarterlyDiscountPercent: plan.quarterlyDiscountPercent || 0,
            yearlyDiscountPercent: plan.yearlyDiscountPercent || 0,
            quarterlyDiscountText: plan.quarterlyDiscountText || '',
            yearlyDiscountText: plan.yearlyDiscountText || ''
          } as SubscriptionPlanDto;
        }
        return null;
      }),
      catchError((error) => {
        console.error('Error loading plan by type:', error);
        return of(null);
      })
    );
  }

  /**
   * Clear the cache
   */
  clearCache(): void {
    this.plansCache$ = undefined;
  }
}