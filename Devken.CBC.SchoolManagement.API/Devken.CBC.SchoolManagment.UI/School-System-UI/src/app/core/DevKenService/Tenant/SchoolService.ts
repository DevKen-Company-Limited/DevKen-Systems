import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { Observable, of, forkJoin } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import {
  ApiResponse,
  CreateSchoolRequest,
  CreateSubscriptionRequest,
  SchoolDto,
  SchoolWithSubscription,
  Subscription,
  SubscriptionStatus,
  SubscriptionStatusCheck,
  UpdateSchoolRequest,
  UpdateSchoolStatusRequest
} from 'app/Tenant/types/school';

@Injectable({ providedIn: 'root' })
export class SchoolService {
  private readonly _http        = inject(HttpClient);
  private readonly _apiBase     = inject(API_BASE_URL);
  private readonly _url         = `${this._apiBase}/api/schools`;
  private readonly _subUrl      = `${this._apiBase}/api/subscription`;

  // ─── School CRUD ─────────────────────────────────────────────────────────

  getAll(): Observable<ApiResponse<SchoolDto[]>> {
    return this._http.get<ApiResponse<SchoolDto[]>>(this._url).pipe(
      tap(r => console.log('[SchoolService] getAll:', r)),
      catchError(this.handleError<SchoolDto[]>('getAll'))
    );
  }

  getById(id: string): Observable<ApiResponse<SchoolDto>> {
    return this._http.get<ApiResponse<SchoolDto>>(`${this._url}/${id}`).pipe(
      catchError(this.handleError<SchoolDto>('getById'))
    );
  }

  create(payload: CreateSchoolRequest): Observable<ApiResponse<SchoolDto>> {
    return this._http.post<ApiResponse<SchoolDto>>(this._url, payload).pipe(
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

  // ─── Logo ─────────────────────────────────────────────────────────────────

  /**
   * Upload a logo image file for the given school.
   * Maps to: POST /api/schools/{id}/logo (multipart/form-data)
   */
  uploadLogo(schoolId: string, file: File): Observable<ApiResponse<{ logoUrl: string }>> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this._http
      .post<ApiResponse<{ logoUrl: string }>>(`${this._url}/${schoolId}/logo`, formData)
      .pipe(
        catchError(this.handleError<{ logoUrl: string }>('uploadLogo'))
      );
  }

  /**
   * Delete the logo for the given school.
   * Maps to: DELETE /api/schools/{id}/logo
   */
  deleteLogo(schoolId: string): Observable<ApiResponse<null>> {
    return this._http.delete<ApiResponse<null>>(`${this._url}/${schoolId}/logo`).pipe(
      catchError(this.handleError<null>('deleteLogo'))
    );
  }

  // ─── Schools + Subscriptions combined ────────────────────────────────────

  getAllWithSubscriptions(): Observable<ApiResponse<SchoolWithSubscription[]>> {
    return forkJoin({
      schools:       this.getAll(),
      subscriptions: this.getAllSubscriptions()
    }).pipe(
      map(({ schools, subscriptions }) => {
        if (!schools.success) {
          return {
            success: false,
            message: schools.message || 'Failed to load schools',
            errors: schools.errors ?? []
          } as ApiResponse<SchoolWithSubscription[]>;
        }

        const subList = subscriptions.success ? subscriptions.data ?? [] : [];

        const merged = (schools.data ?? []).map(school => {
          const sub = subList.find(s => s.schoolId === school.id) ?? null;
          return {
            ...school,
            subscription:          sub,
            subscriptionStatus:    sub?.status ?? null,
            subscriptionExpiry:    sub?.expiryDate ?? null,
            isSubscriptionActive:  sub
              ? sub.status === SubscriptionStatus.Active && sub.canAccess
              : false,
            isSubscriptionExpired: sub
              ? sub.status === SubscriptionStatus.Expired || !!sub.isExpired
              : true,
            daysRemaining:         sub?.daysRemaining ?? 0,
            needsSubscription:     !sub || sub.status === SubscriptionStatus.Expired || !!sub.isExpired
          } as SchoolWithSubscription;
        });

        return { success: true, data: merged, message: 'Schools loaded successfully' };
      }),
      catchError(err => {
        console.error('[SchoolService] getAllWithSubscriptions:', err);
        return of({
          success: false,
          message: 'Failed to load schools with subscriptions',
          errors: [err.message ?? 'Unknown error'],
          data: []
        } as ApiResponse<SchoolWithSubscription[]>);
      })
    );
  }

  getSchoolWithSubscription(id: string): Observable<ApiResponse<SchoolWithSubscription>> {
    return forkJoin({
      school:       this.getById(id),
      subscription: this.getSchoolSubscription(id)
    }).pipe(
      map(({ school, subscription }) => {
        if (!school.success || !school.data) {
          return {
            success: false,
            message: school.message ?? 'School not found',
            errors: school.errors
          } as ApiResponse<SchoolWithSubscription>;
        }

        const sub = subscription.success ? subscription.data ?? null : null;

        return {
          success: true,
          data: {
            ...school.data,
            subscription:          sub,
            subscriptionStatus:    sub?.status ?? null,
            subscriptionExpiry:    sub?.expiryDate ?? null,
            isSubscriptionActive:  sub ? sub.status === SubscriptionStatus.Active && sub.canAccess : false,
            isSubscriptionExpired: sub ? sub.status === SubscriptionStatus.Expired || !!sub.isExpired : true,
            daysRemaining:         sub?.daysRemaining ?? 0,
            needsSubscription:     !sub || sub.status === SubscriptionStatus.Expired || !!sub.isExpired
          } as SchoolWithSubscription,
          message: 'School loaded successfully'
        };
      }),
      catchError(err => of({
        success: false,
        message: 'Failed to load school',
        errors: [err.message]
      } as ApiResponse<SchoolWithSubscription>))
    );
  }

  // ─── Subscriptions ────────────────────────────────────────────────────────

  getAllSubscriptions(): Observable<ApiResponse<Subscription[]>> {
    return this._http.get<ApiResponse<Subscription[]>>(`${this._subUrl}/all`).pipe(
      catchError(() => of({ success: false, data: [], message: 'Failed to load subscriptions' } as ApiResponse<Subscription[]>))
    );
  }

  getSchoolSubscription(schoolId: string): Observable<ApiResponse<Subscription>> {
    return this._http.get<ApiResponse<Subscription>>(`${this._subUrl}/school/${schoolId}`).pipe(
      catchError(err => {
        if (err.status === 404) {
          return of({ success: false, data: null as any, message: 'No subscription found' } as ApiResponse<Subscription>);
        }
        return of({ success: false, message: 'Failed to load subscription', errors: [err.message] } as ApiResponse<Subscription>);
      })
    );
  }

  createSubscription(schoolId: string, payload: CreateSubscriptionRequest): Observable<ApiResponse<Subscription>> {
    return this._http.post<ApiResponse<Subscription>>(this._subUrl, {
      ...payload, schoolId, plan: Number(payload.plan), billingCycle: Number(payload.billingCycle)
    }).pipe(catchError(this.handleError<Subscription>('createSubscription')));
  }

  updateSubscription(id: string, payload: any): Observable<ApiResponse<Subscription>> {
    return this._http.put<ApiResponse<Subscription>>(`${this._subUrl}/${id}`, payload).pipe(
      catchError(this.handleError<Subscription>('updateSubscription'))
    );
  }

  activateSubscription(id: string): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(`${this._subUrl}/${id}/activate`, {}).pipe(
      catchError(this.handleError('activateSubscription'))
    );
  }

  suspendSubscription(id: string, reason: string): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(`${this._subUrl}/${id}/suspend`, { reason }).pipe(
      catchError(this.handleError('suspendSubscription'))
    );
  }

  cancelSubscription(id: string): Observable<ApiResponse<any>> {
    return this._http.post<ApiResponse<any>>(`${this._subUrl}/${id}/cancel`, {}).pipe(
      catchError(this.handleError('cancelSubscription'))
    );
  }

  renewSubscription(id: string, billingCycle: number): Observable<ApiResponse<Subscription>> {
    return this._http.post<ApiResponse<Subscription>>(`${this._subUrl}/${id}/renew`, { billingCycle: Number(billingCycle) }).pipe(
      catchError(this.handleError<Subscription>('renewSubscription'))
    );
  }

  // ─── Status Check ─────────────────────────────────────────────────────────

  checkSubscriptionStatus(schoolId: string): Observable<SubscriptionStatusCheck> {
    return this.getSchoolSubscription(schoolId).pipe(
      map(response => {
        if (!response.success || !response.data) {
          return {
            hasActiveSubscription: false, status: 'No Subscription',
            daysRemaining: 0, message: 'No subscription found for this school',
            needsRenewal: true, isExpired: true, isInGracePeriod: false, canAccess: false
          };
        }

        const sub = response.data;
        const daysRemaining = Math.ceil(
          (new Date(sub.expiryDate).getTime() - Date.now()) / 86_400_000
        );
        const isExpired       = sub.isExpired || daysRemaining <= 0;
        const isInGracePeriod = sub.status === SubscriptionStatus.GracePeriod;
        const isActive        = sub.status === SubscriptionStatus.Active;
        const canAccess       = isActive && !isExpired;

        const message = isExpired
          ? 'Subscription expired. Please renew to continue.'
          : isInGracePeriod
            ? 'Grace period active. Renew to avoid interruption.'
            : `Active – expires in ${daysRemaining} day${daysRemaining !== 1 ? 's' : ''}.`;

        return {
          hasActiveSubscription: canAccess,
          status: this.getStatusLabel(sub.status),
          expiryDate: sub.expiryDate,
          daysRemaining: Math.max(0, daysRemaining),
          message, needsRenewal: daysRemaining <= 7 || isExpired || isInGracePeriod,
          isExpired, isInGracePeriod, canAccess
        };
      }),
      catchError(() => of({
        hasActiveSubscription: false, status: 'Error', daysRemaining: 0,
        message: 'Unable to check status. Please try again.',
        needsRenewal: true, isExpired: true, isInGracePeriod: false, canAccess: false
      }))
    );
  }

  // ─── Display Helpers ──────────────────────────────────────────────────────

  generateSlug(name: string): string {
    return name.toLowerCase()
      .replace(/[^\w\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .replace(/^-|-$/g, '');
  }

  getSubscriptionPlanDisplay(plan: number): string {
    const labels: Record<number, string> = { 0: 'Basic', 1: 'Standard', 2: 'Premium', 3: 'Enterprise' };
    return labels[plan] ?? `Plan ${plan}`;
  }

  getBillingCycleDisplay(cycle: number): string {
    const labels: Record<number, string> = { 1: 'Monthly', 3: 'Quarterly', 4: 'Yearly' };
    return labels[cycle] ?? `${cycle} months`;
  }

  private getStatusLabel(status: number): string {
    const labels: Record<number, string> = {
      0: 'Pending Payment', 1: 'Active', 2: 'Suspended',
      3: 'Cancelled', 4: 'Expired', 5: 'Grace Period'
    };
    return labels[status] ?? 'Unknown';
  }

  // ─── Error Handler ────────────────────────────────────────────────────────

  private handleError<T>(op = 'operation') {
    return (error: HttpErrorResponse): Observable<ApiResponse<T>> => {
      console.error(`[SchoolService] ${op} failed:`, error);

      const message = error.error?.message
        ?? (error.status === 0   ? 'Cannot connect to server'
          : error.status === 401 ? 'Unauthorized'
          : error.status === 403 ? 'Forbidden – insufficient permissions'
          : error.status === 404 ? 'Resource not found'
          : error.status >= 500  ? 'Server error'
          : error.message);

      return of({
        success: false,
        message,
        errors: error.error?.errors ?? [message]
      } as ApiResponse<T>);
    };
  }
}