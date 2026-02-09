import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable, of, forkJoin, throwError } from 'rxjs';
import { map, switchMap, catchError, tap } from 'rxjs/operators';
import {
  ApiResponse,
  CreateSchoolRequest,
  CreateSubscriptionRequest,
  SchoolDto,
  SchoolStats,
  SchoolWithSubscription,
  Subscription,
  SubscriptionStatus,
  SubscriptionStatusCheck,
  UpdateSchoolRequest,
  UpdateSchoolStatusRequest
} from 'app/Tenant/types/school';

@Injectable({
  providedIn: 'root'
})
export class SchoolService {
  private readonly _http = inject(HttpClient);
  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _url = `${this._apiBase}/api/schools`;
  private readonly _subscriptionUrl = `${this._apiBase}/api/subscription`;

  // ==================== School CRUD ====================

  getAll(): Observable<ApiResponse<SchoolDto[]>> {
    return this._http.get<ApiResponse<SchoolDto[]>>(`${this._url}`).pipe(
      tap(response => console.log('Schools API response:', response)),
      catchError(this.handleError<SchoolDto[]>('getAll'))
    );
  }

  getAllWithSubscriptions(): Observable<ApiResponse<SchoolWithSubscription[]>> {
    return forkJoin({
      schools: this.getAll(),
      subscriptions: this.getAllSubscriptions()
    }).pipe(
      map(({ schools, subscriptions }) => {
        if (!schools.success) {
          return {
            success: false,
            message: schools.message || 'Failed to load schools',
            errors: schools.errors || []
          } as ApiResponse<SchoolWithSubscription[]>;
        }

        const subscriptionList = subscriptions.success ? subscriptions.data || [] : [];

        const schoolsWithSubs = (schools.data || []).map(school => {
          const subscription = subscriptionList.find(sub => sub.schoolId === school.id);
          
          return {
            ...school,
            subscription: subscription || null,
            subscriptionStatus: subscription?.status || null,
            subscriptionExpiry: subscription?.expiryDate || null,
            isSubscriptionActive: subscription ? (subscription.status === SubscriptionStatus.Active && subscription.canAccess) : false,
            isSubscriptionExpired: subscription ? (subscription.status === SubscriptionStatus.Expired || subscription.isExpired) : true,
            daysRemaining: subscription?.daysRemaining || 0,
            needsSubscription: !subscription || (subscription.status === SubscriptionStatus.Expired || subscription.isExpired)
          } as SchoolWithSubscription;
        });

        return {
          success: true,
          data: schoolsWithSubs,
          message: 'Schools with subscriptions loaded successfully'
        };
      }),
      catchError(error => {
        console.error('Error loading schools with subscriptions:', error);
        return of({
          success: false,
          message: 'Failed to load schools with subscriptions',
          errors: [error.message || 'Unknown error'],
          data: []
        } as ApiResponse<SchoolWithSubscription[]>);
      })
    );
  }

  getById(id: string): Observable<ApiResponse<SchoolDto>> {
    return this._http.get<ApiResponse<SchoolDto>>(`${this._url}/${id}`).pipe(
      catchError(this.handleError<SchoolDto>('getById'))
    );
  }

  getSchoolWithSubscription(id: string): Observable<ApiResponse<SchoolWithSubscription>> {
    return forkJoin({
      school: this.getById(id),
      subscription: this.getSchoolSubscription(id)
    }).pipe(
      map(({ school, subscription }) => {
        if (!school.success || !school.data) {
          return {
            success: false,
            message: school.message || 'School not found',
            errors: school.errors
          } as ApiResponse<SchoolWithSubscription>;
        }

        const sub = subscription.success ? subscription.data : null;

        const schoolWithSub: SchoolWithSubscription = {
          ...school.data,
          subscription: sub,
          subscriptionStatus: sub?.status || null,
          subscriptionExpiry: sub?.expiryDate || null,
          isSubscriptionActive: sub ? (sub.status === SubscriptionStatus.Active && sub.canAccess) : false,
          isSubscriptionExpired: sub ? (sub.status === SubscriptionStatus.Expired || sub.isExpired) : true,
          daysRemaining: sub?.daysRemaining || 0,
          needsSubscription: !sub || (sub.status === SubscriptionStatus.Expired || sub.isExpired),
          createdOn: '',
          phoneNumber: undefined
        };

        return {
          success: true,
          data: schoolWithSub,
          message: 'School with subscription loaded successfully'
        };
      }),
      catchError(error => {
        console.error('Error loading school with subscription:', error);
        return of({
          success: false,
          message: 'Failed to load school with subscription',
          errors: [error.message]
        } as ApiResponse<SchoolWithSubscription>);
      })
    );
  }
  // ==================== Utility Methods ====================

/**
 * Generate a URL-friendly slug from a school name
 */
generateSlug(name: string): string {
  if (!name) return '';
  
  return name
    .toLowerCase()
    .replace(/[^\w\s-]/g, '')    // Remove non-word characters
    .replace(/\s+/g, '-')        // Replace spaces with hyphens
    .replace(/-+/g, '-')         // Replace multiple hyphens with single hyphen
    .trim();                     // Trim leading/trailing hyphens
}

/**
 * Get display name for subscription plan
 */
getSubscriptionPlanDisplay(plan: number): string {
  // Assuming these are your subscription plan codes
  // You may need to adjust these based on your actual plan definitions
  switch (plan) {
    case 1: return 'Free';
    case 2: return 'Basic';
    case 3: return 'Pro';
    case 4: return 'Enterprise';
    case 5: return 'Custom';
    default: return `Plan ${plan}`;
  }
}

/**
 * Get display name for billing cycle
 */
getBillingCycleDisplay(billingCycle: number): string {
  switch (billingCycle) {
    case 1: return 'Monthly';
    case 3: return 'Quarterly';
    case 6: return 'Semi-Annually';
    case 12: return 'Annually';
    default: return `${billingCycle} Months`;
  }
}

  create(payload: CreateSchoolRequest): Observable<ApiResponse<SchoolDto>> {
    return this._http.post<ApiResponse<SchoolDto>>(`${this._url}`, payload).pipe(
      catchError(this.handleError<SchoolDto>('create'))
    );
  }

  update(id: string, payload: UpdateSchoolRequest): Observable<ApiResponse<SchoolDto>> {
    return this._http.put<ApiResponse<SchoolDto>>(`${this._url}/${id}`, payload).pipe(
      catchError(this.handleError<SchoolDto>('update'))
    );
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(`${this._url}/${id}`).pipe(
      catchError(this.handleError<null>('delete'))
    );
  }

  updateStatus(id: string, isActive: boolean): Observable<ApiResponse<SchoolDto>> {
    const payload: UpdateSchoolStatusRequest = { isActive };
    return this._http.patch<ApiResponse<SchoolDto>>(`${this._url}/${id}/status`, payload).pipe(
      catchError(this.handleError<SchoolDto>('updateStatus'))
    );
  }

  // ==================== Subscriptions ====================

  getAllSubscriptions(): Observable<ApiResponse<Subscription[]>> {
    return this._http.get<ApiResponse<Subscription[]>>(`${this._subscriptionUrl}/all`).pipe(
      tap(response => console.log('All subscriptions API response:', response)),
      catchError(error => {
        console.error('Error fetching subscriptions:', error);
        return of({
          success: false,
          data: [],
          message: 'Failed to load subscriptions',
          errors: [error.message]
        } as ApiResponse<Subscription[]>);
      })
    );
  }

  getSchoolSubscription(schoolId: string): Observable<ApiResponse<Subscription>> {
    return this._http.get<ApiResponse<Subscription>>(`${this._subscriptionUrl}/school/${schoolId}`).pipe(
      catchError(error => {
        if (error.status === 404) {
          return of({
            success: false,
            data: null as any,
            message: 'No subscription found'
          } as ApiResponse<Subscription>);
        }
        return of({
          success: false,
          message: 'Failed to load subscription',
          errors: [error.message]
        } as ApiResponse<Subscription>);
      })
    );
  }

  createSubscription(schoolId: string, payload: CreateSubscriptionRequest): Observable<ApiResponse<Subscription>> {
    const body = {
      ...payload,
      schoolId,
      plan: Number(payload.plan),
      billingCycle: Number(payload.billingCycle)
    };

    return this._http.post<ApiResponse<Subscription>>(this._subscriptionUrl, body).pipe(
      catchError(this.handleError<Subscription>('createSubscription'))
    );
  }

  updateSubscription(subscriptionId: string, payload: any): Observable<ApiResponse<Subscription>> {
    return this._http.put<ApiResponse<Subscription>>(`${this._subscriptionUrl}/${subscriptionId}`, payload).pipe(
      catchError(this.handleError<Subscription>('updateSubscription'))
    );
  }

  activateSubscription(subscriptionId: string): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(`${this._subscriptionUrl}/${subscriptionId}/activate`, {}).pipe(
      catchError(this.handleError<any>('activateSubscription'))
    );
  }

  suspendSubscription(subscriptionId: string, reason: string): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(`${this._subscriptionUrl}/${subscriptionId}/suspend`, { reason }).pipe(
      catchError(this.handleError<any>('suspendSubscription'))
    );
  }

  cancelSubscription(subscriptionId: string): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(`${this._subscriptionUrl}/${subscriptionId}/cancel`, {}).pipe(
      catchError(this.handleError<any>('cancelSubscription'))
    );
  }

  renewSubscription(subscriptionId: string, billingCycle: number): Observable<ApiResponse<Subscription>> {
    return this._http.post<ApiResponse<Subscription>>(
      `${this._subscriptionUrl}/${subscriptionId}/renew`,
      { billingCycle: Number(billingCycle) }
    ).pipe(
      catchError(this.handleError<Subscription>('renewSubscription'))
    );
  }

  // ==================== Status Checks ====================

  checkSubscriptionStatus(schoolId: string): Observable<SubscriptionStatusCheck> {
    return this.getSchoolSubscription(schoolId).pipe(
      map(response => {
        if (!response.success || !response.data) {
          return {
            hasActiveSubscription: false,
            status: 'No Subscription',
            daysRemaining: 0,
            message: 'No subscription found for this school',
            needsRenewal: true,
            isExpired: true,
            isInGracePeriod: false,
            canAccess: false
          };
        }

        const subscription = response.data;
        const today = new Date();
        const expiryDate = new Date(subscription.expiryDate);
        const daysRemaining = Math.ceil((expiryDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

        const isActive = subscription.status === SubscriptionStatus.Active;
        const isExpired = subscription.isExpired || daysRemaining <= 0;
        const isInGracePeriod = subscription.status === SubscriptionStatus.GracePeriod;

        let message = '';
        const canAccess = isActive && !isExpired;

        if (isActive && daysRemaining > 0) {
          message = `Subscription active. Expires in ${daysRemaining} day${daysRemaining !== 1 ? 's' : ''}.`;
        } else if (isExpired) {
          message = 'Subscription has expired. Please renew to continue service.';
        } else if (isInGracePeriod) {
          message = 'Subscription is in grace period. Please renew to avoid service interruption.';
        }

        return {
          hasActiveSubscription: isActive && !isExpired,
          status: this.getStatusName(subscription.status),
          expiryDate: subscription.expiryDate,
          daysRemaining: daysRemaining > 0 ? daysRemaining : 0,
          message,
          needsRenewal: daysRemaining <= 7 || isExpired || isInGracePeriod,
          isExpired,
          isInGracePeriod,
          canAccess
        };
      }),
      catchError(error => {
        console.error('Error checking subscription status:', error);
        return of({
          hasActiveSubscription: false,
          status: 'Error',
          daysRemaining: 0,
          message: 'Unable to check subscription status. Please try again later.',
          needsRenewal: true,
          isExpired: true,
          isInGracePeriod: false,
          canAccess: false
        });
      })
    );
  }

  // ==================== Utilities ====================

  private getStatusName(status: number): string {
    switch (status) {
      case SubscriptionStatus.Active:
        return 'Active';
      case SubscriptionStatus.Expired:
        return 'Expired';
      case SubscriptionStatus.Suspended:
        return 'Suspended';
      case SubscriptionStatus.Cancelled:
        return 'Cancelled';
      case SubscriptionStatus.GracePeriod:
        return 'Grace Period';
      case SubscriptionStatus.PendingPayment:
        return 'Pending Payment';
      default:
        return 'Unknown';
    }
  }

  private handleError<T>(operation = 'operation') {
    return (error: HttpErrorResponse): Observable<ApiResponse<T>> => {
      console.error(`${operation} failed:`, error);

      let errorMessage = 'An error occurred';
      if (error.error?.message) {
        errorMessage = error.error.message;
      } else if (error.message) {
        errorMessage = error.message;
      } else if (error.status === 0) {
        errorMessage = 'Unable to connect to server';
      } else if (error.status === 401) {
        errorMessage = 'Unauthorized access';
      } else if (error.status === 403) {
        errorMessage = 'Forbidden - insufficient permissions';
      } else if (error.status === 404) {
        errorMessage = 'Resource not found';
      } else if (error.status >= 500) {
        errorMessage = 'Server error occurred';
      }

      return of({
        success: false,
        message: errorMessage,
        errors: error.error?.errors || [errorMessage]
      } as ApiResponse<T>);
    };
  }
}