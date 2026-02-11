import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError, shareReplay } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';

export interface EnumItemDto {
  id: string;
  name: string;
  value: number;
  description?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EnumService {
  private baseUrl = `${inject(API_BASE_URL)}/api/enums`;

  // Cache to prevent multiple requests for the same enum
  private cache: Record<string, Observable<EnumItemDto[]>> = {};

  constructor(private http: HttpClient) {}

  /** Generic method to fetch any enum from backend */
  private fetchEnum(endpoint: string): Observable<EnumItemDto[]> {
    if (!this.cache[endpoint]) {
      this.cache[endpoint] = this.http.get<any>(`${this.baseUrl}/${endpoint}`).pipe(
        map(res => res.success && res.data ? res.data as EnumItemDto[] : []),
        catchError(err => {
          console.error(`Error loading enum ${endpoint}:`, err);
          return of([]);
        }),
        shareReplay(1) // Cache the observable
      );
    }
    return this.cache[endpoint];
  }

  /** ===== SUBSCRIPTION ===== */
  getSubscriptionPlans() { return this.fetchEnum('subscription-plans'); }
  getSubscriptionStatuses() { return this.fetchEnum('subscription-statuses'); }
  getBillingCycles() { return this.fetchEnum('billing-cycles'); }

  /** ===== STUDENTS ===== */
  getGenders() { return this.fetchEnum('genders'); }
  getStudentStatuses() { return this.fetchEnum('student-statuses'); }
  getCBCLevels() { return this.fetchEnum('cbc-levels'); }

  /** ===== ACADEMICS ===== */
  getTermTypes() { return this.fetchEnum('term-types'); }
  getAssessmentTypes() { return this.fetchEnum('assessment-types'); }
  getCompetencyLevels() { return this.fetchEnum('competency-levels'); }

  /** ===== TEACHER ===== */
  getTeacherEmploymentTypes() { return this.fetchEnum('teacher-employment-types'); }
  getTeacherDesignations() { return this.fetchEnum('teacher-designations'); }

  /** ===== PAYMENTS ===== */
  getPaymentStatuses() { return this.fetchEnum('payment-statuses'); }
  getMpesaPaymentStatuses() { return this.fetchEnum('mpesa-payment-statuses'); }
  getMpesaResultCodes() { return this.fetchEnum('mpesa-result-codes'); }

  /** ===== ENTITY ===== */
  getEntityStatuses() { return this.fetchEnum('entity-statuses'); }

  /** Clear cache for all enums */
  clearCache() { this.cache = {}; }
}
