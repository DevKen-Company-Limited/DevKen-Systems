// ═══════════════════════════════════════════════════════════════════
// pesapal-settings.service.ts
// Place in: src/app/core/DevKenService/payments/
// ═══════════════════════════════════════════════════════════════════

import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import {
  PesaPalSettingsDto,
  PesaPalIpnRegistrationResult,
} from '../../../payments/pesaPall/pesapal.types';

/** Shape of every response your .NET BaseApiController returns */
interface ApiEnvelope<T> {
  success: boolean;
  message: string | null;
  data: T;
  statusCode: number;
}

@Injectable({ providedIn: 'root' })
export class PesaPalSettingsService {
  private readonly _http    = inject(HttpClient);
  private readonly _apiBase = inject(API_BASE_URL);
  private readonly _base    = `${this._apiBase}/api/pesapal`;

  /** Load current settings — unwraps the { success, data } envelope. */
  getSettings(): Observable<PesaPalSettingsDto> {
    return this._http
      .get<ApiEnvelope<PesaPalSettingsDto>>(`${this._base}/settings`)
      .pipe(map(r => r.data));
  }

  /** Persist settings server-side — unwraps envelope. */
  saveSettings(dto: PesaPalSettingsDto): Observable<void> {
    return this._http
      .put<ApiEnvelope<void>>(`${this._base}/settings`, dto)
      .pipe(map(() => void 0));
  }

  /** Trigger IPN registration (idempotent). */
  registerIpn(): Observable<PesaPalIpnRegistrationResult> {
    return this._http
      .post<ApiEnvelope<PesaPalIpnRegistrationResult>>(
        `${this._base}/ipn/register`,
        {}
      )
      .pipe(map(r => r.data));
  }
}