// ═══════════════════════════════════════════════════════════════════
// enum.service.ts
//
// Rules:
//  • Uses inject() pattern — no constructor parameters
//  • Caches each endpoint with shareReplay(1) — zero duplicate calls
//  • Every public method has an explicit return type
//  • Added assessment-specific enums needed by the assessment module
//  • Added toOptions() and labelFor() utility helpers so components
//    never need to build option arrays or resolve labels by hand
//
// ENUM VALUE CONTRACT (matches C# backend):
//  NUMBER enums → send dto.value     → use toOptions(items)
//    assessmentType   : Formative=1 | Summative=2 | Competency=3
//    assessmentMethod : Observation=0 | Written=1 | Oral=2
//                       Practical=3 | Portfolio=4 | PeerAssessment=5
//    cbcLevel         : PP1=1 | PP2=2 | Grade1=3 … Grade9=11
//
//  STRING enums → send dto.id        → use toOptions(items, true)
//    formativeType  : "Quiz" | "Homework" | "ClassActivity" | …
//    examType       : "MidTerm" | "EndTerm" | "Mock" | "CAT" | "Final"
//    ratingScale    : "EE-ME-AE-BE" | "1-4" | "1-5" | "Pass/Fail"
//    performanceLevel / rating : "EE" | "ME" | "AE" | "BE"
// ═══════════════════════════════════════════════════════════════════

import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError, shareReplay } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';

// ─── DTO returned by every GET /api/enums/{endpoint} ─────────────
export interface EnumItemDto {
  /** String key / name — e.g. "Formative", "Quiz", "EE"             */
  id:           string;
  /** Human-readable display label shown in the UI                   */
  name:         string;
  /** Numeric enum value — sent to the API for number-based enums    */
  value:        number;
  /** Optional sub-label rendered next to the option in mat-select   */
  description?: string;
}

// ─── mat-select helper type ───────────────────────────────────────
export interface SelectOption {
  label: string;
  value: number | string;
}

@Injectable({ providedIn: 'root' })
export class EnumService {

  private readonly _base = `${inject(API_BASE_URL)}/api/enums`;
  private readonly _http = inject(HttpClient);

  /** Per-endpoint observable cache — prevents duplicate HTTP calls  */
  private _cache: Record<string, Observable<EnumItemDto[]>> = {};

  // ── Core private fetcher ─────────────────────────────────────────
  private _fetch(endpoint: string): Observable<EnumItemDto[]> {
    if (!this._cache[endpoint]) {
      this._cache[endpoint] = this._http
        .get<{ success: boolean; data: EnumItemDto[] }>(`${this._base}/${endpoint}`)
        .pipe(
          map(res => (res.success && res.data ? res.data : [])),
          catchError(err => {
            console.error(`[EnumService] Failed to load enum "${endpoint}":`, err);
            return of([] as EnumItemDto[]);
          }),
          shareReplay(1),
        );
    }
    return this._cache[endpoint];
  }

  // ════════════════════════════════════════════════════════════════
  // UTILITY HELPERS
  // ════════════════════════════════════════════════════════════════

  /**
   * Converts EnumItemDto[] → SelectOption[] ready for mat-select.
   *
   * @param items  Array returned by any get*() method
   * @param useId  false (default) → option.value = dto.value  (number)
   *                                 Use for number-based enums
   *               true            → option.value = dto.id     (string)
   *                                 Use for string-based enums
   *
   * @example — number enum (assessmentType, cbcLevel, assessmentMethod)
   *   this.enumSvc.getAssessmentTypes().subscribe(items =>
   *     this.typeOpts = this.enumSvc.toOptions(items));
   *   // mat-option [value]="opt.value" stores 1 / 2 / 3
   *
   * @example — string enum (formativeType, examType, ratingScale)
   *   this.enumSvc.getFormativeTypes().subscribe(items =>
   *     this.ftOpts = this.enumSvc.toOptions(items, true));
   *   // mat-option [value]="opt.value" stores "Quiz" / "Homework" / …
   */
  toOptions(items: EnumItemDto[], useId = false): SelectOption[] {
    return items.map(i => ({ label: i.name, value: useId ? i.id : i.value }));
  }

  /**
   * Resolves a stored enum value back to its readable label.
   * Accepts both numeric (matched by dto.value) and string (matched
   * by dto.id) values so it works for all enum types.
   *
   * @example
   *   this.enumSvc.labelFor(cbcItems,        3)       // "Grade 1"
   *   this.enumSvc.labelFor(formativeItems, 'Quiz')   // "Quiz Assessment"
   *   this.enumSvc.labelFor(methodItems,    0)        // "Observation"
   */
  labelFor(items: EnumItemDto[], value: number | string): string {
    const match = items.find(i => i.value === Number(value) || i.id === String(value));
    return match?.name ?? String(value);
  }

  /** Clear the entire cache (e.g. after login / logout / school switch). */
  clearCache(): void { this._cache = {}; }

  /** Clear cache for a single endpoint only. */
  clearCacheFor(endpoint: string): void { delete this._cache[endpoint]; }

  // ════════════════════════════════════════════════════════════════
  // SUBSCRIPTION
  // ════════════════════════════════════════════════════════════════

  /** SubscriptionPlan enum */
  getSubscriptionPlans(): Observable<EnumItemDto[]>    { return this._fetch('subscription-plans');    }

  /** SubscriptionStatus enum */
  getSubscriptionStatuses(): Observable<EnumItemDto[]> { return this._fetch('subscription-statuses'); }

  /** BillingCycle enum */
  getBillingCycles(): Observable<EnumItemDto[]>        { return this._fetch('billing-cycles');        }

  // ════════════════════════════════════════════════════════════════
  // STUDENTS
  // ════════════════════════════════════════════════════════════════

  /** Gender — Male=0, Female=1, Other=2 */
  getGenders(): Observable<EnumItemDto[]>          { return this._fetch('genders');          }

  /** StudentStatus enum */
  getStudentStatuses(): Observable<EnumItemDto[]>  { return this._fetch('student-statuses'); }

  /**
   * CBCLevel — PP1=1, PP2=2, Grade1=3 … Grade9=11
   * Also used as `targetLevel` on Competency assessments.
   * → value is a NUMBER — use toOptions(items) (default).
   */
  getCBCLevels(): Observable<EnumItemDto[]>        { return this._fetch('cbc-levels');       }

  /** Religion enum */
  getReligions(): Observable<EnumItemDto[]>        { return this._fetch('religions');        }

  /** Nationality enum */
  getNationalities(): Observable<EnumItemDto[]>    { return this._fetch('nationalities');    }

  // ════════════════════════════════════════════════════════════════
  // ACADEMICS — GENERAL
  // ════════════════════════════════════════════════════════════════

  /** TermType — Term1, Term2, Term3 */
  getTermTypes(): Observable<EnumItemDto[]>          { return this._fetch('term-types');        }

  /**
   * AssessmentType — Formative=1 | Summative=2 | Competency=3
   * → value is a NUMBER — use toOptions(items) (default).
   * ⚠ NEVER send the string name ("Formative") to the API.
   */
  getAssessmentTypes(): Observable<EnumItemDto[]>    { return this._fetch('assessment-types'); }

  /**
   * CompetencyLevel / PerformanceLevel — "EE" | "ME" | "AE" | "BE"
   * → value is a STRING — use toOptions(items, true).
   * Used as `performanceLevel` (Formative score) or `rating` (Competency score).
   */
  getCompetencyLevels(): Observable<EnumItemDto[]>   { return this._fetch('competency-levels'); }

  // ════════════════════════════════════════════════════════════════
  // ACADEMICS — ASSESSMENT SPECIFIC
  // ════════════════════════════════════════════════════════════════

  /**
   * AssessmentMethod — Observation=0 | Written=1 | Oral=2
   *                    Practical=3  | Portfolio=4 | PeerAssessment=5
   * Used on Competency assessments only.
   * → value is a NUMBER — use toOptions(items) (default).
   */
  getAssessmentMethods(): Observable<EnumItemDto[]>  { return this._fetch('assessment-methods'); }

  /**
   * FormativeType — "Quiz" | "Homework" | "ClassActivity" | "Project"
   *                 "Observation" | "Portfolio" | "PeerAssessment"
   * → value is a STRING — use toOptions(items, true).
   */
  getFormativeTypes(): Observable<EnumItemDto[]>     { return this._fetch('formative-types');    }

  /**
   * ExamType — "MidTerm" | "EndTerm" | "Mock" | "CAT" | "Final"
   * → value is a STRING — use toOptions(items, true).
   */
  getExamTypes(): Observable<EnumItemDto[]>           { return this._fetch('exam-types');         }

  /**
   * RatingScale — "EE-ME-AE-BE" | "1-4" | "1-5" | "Pass/Fail"
   * → value is a STRING — use toOptions(items, true).
   */
  getRatingScales(): Observable<EnumItemDto[]>        { return this._fetch('rating-scales');      }

  // ════════════════════════════════════════════════════════════════
  // TEACHER
  // ════════════════════════════════════════════════════════════════

  /** TeacherEmploymentType enum */
  getTeacherEmploymentTypes(): Observable<EnumItemDto[]> { return this._fetch('teacher-employment-types'); }

  /** TeacherDesignation enum */
  getTeacherDesignations(): Observable<EnumItemDto[]>    { return this._fetch('teacher-designations');     }

  // ════════════════════════════════════════════════════════════════
  // PAYMENTS
  // ════════════════════════════════════════════════════════════════

  /** PaymentStatus enum */
  getPaymentStatuses(): Observable<EnumItemDto[]>       { return this._fetch('payment-statuses');        }

  /** MpesaPaymentStatus enum */
  getMpesaPaymentStatuses(): Observable<EnumItemDto[]>  { return this._fetch('mpesa-payment-statuses');  }

  /** MpesaResultCode enum */
  getMpesaResultCodes(): Observable<EnumItemDto[]>      { return this._fetch('mpesa-result-codes');      }

  // ════════════════════════════════════════════════════════════════
  // ENTITY / GENERAL
  // ════════════════════════════════════════════════════════════════

  /** EntityStatus — Active, Inactive, Suspended … */
  getEntityStatuses(): Observable<EnumItemDto[]>  { return this._fetch('entity-statuses'); }

  /** GradeType enum */
  getGradeTypes(): Observable<EnumItemDto[]>      { return this._fetch('grade-types');     }

  /** GradeLetter — A, A-, B+, B … F */
  getGradeLetters(): Observable<EnumItemDto[]>    { return this._fetch('grade-letters');   }
}