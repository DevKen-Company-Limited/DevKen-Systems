// import { inject, Injectable } from '@angular/core';
// import { HttpClient, HttpParams } from '@angular/common/http';
// import { Observable } from 'rxjs';
// import { API_BASE_URL } from 'app/app.config';
// import { ApiResponse } from 'app/Tenant/types/school';
// import {
//   CreateInvoiceItemDto,
//   InvoiceItemResponseDto,
//   UpdateInvoiceItemDto,
// } from 'app/Finance/Invoice-items/Types/invoice-items.types';

// @Injectable({ providedIn: 'root' })
// export class InvoiceItemService {
//   private readonly apiBase = inject(API_BASE_URL);
//   private readonly base    = `${this.apiBase}/api/finance/invoiceitems`;

//   private readonly http    = inject(HttpClient);


//   // // Base: /api/finance/invoiceitems
//   // private get base(): string {
//   //   return `${this.apiBase}/api/finance/invoiceitems`;
//   // }

//   // ── Queries ───────────────────────────────────────────────────────────────

//   /**
//    * GET /api/finance/invoiceitems
//    * SuperAdmin: pass schoolId to scope, or omit for all schools.
//    * Regular user: schoolId is ignored (scoped server-side).
//    * Optionally filter by invoiceId.
//    */
//   getAll(
//     schoolId?: string,
//     invoiceId?: string,
//   ): Observable<ApiResponse<InvoiceItemResponseDto[]>> {
//     let params = new HttpParams();
//     if (schoolId)  params = params.set('schoolId',  schoolId);
//     if (invoiceId) params = params.set('invoiceId', invoiceId);
//     return this.http.get<ApiResponse<InvoiceItemResponseDto[]>>(this.base, { params });
//   }

//   /**
//    * GET /api/finance/invoiceitems/{id}
//    */
//   getById(id: string): Observable<ApiResponse<InvoiceItemResponseDto>> {
//     return this.http.get<ApiResponse<InvoiceItemResponseDto>>(`${this.base}/${id}`);
//   }

//   /**
//    * GET /api/finance/invoiceitems/by-invoice/{invoiceId}
//    */
//   getByInvoice(invoiceId: string): Observable<ApiResponse<InvoiceItemResponseDto[]>> {
//     return this.http.get<ApiResponse<InvoiceItemResponseDto[]>>(
//       `${this.base}/by-invoice/${invoiceId}`,
//     );
//   }

//   // ── Mutations ─────────────────────────────────────────────────────────────

//   /**
//    * POST /api/finance/invoiceitems
//    * TenantId is set server-side for regular users.
//    * SuperAdmin must populate dto.tenantId before calling.
//    */
//   create(
//     payload: CreateInvoiceItemDto,
//   ): Observable<ApiResponse<InvoiceItemResponseDto>> {
//     return this.http.post<ApiResponse<InvoiceItemResponseDto>>(this.base, payload);
//   }

//   /**
//    * PUT /api/finance/invoiceitems/{id}
//    */
//   update(
//     id: string,
//     payload: UpdateInvoiceItemDto,
//   ): Observable<ApiResponse<InvoiceItemResponseDto>> {
//     return this.http.put<ApiResponse<InvoiceItemResponseDto>>(
//       `${this.base}/${id}`,
//       payload,
//     );
//   }

//   /**
//    * DELETE /api/finance/invoiceitems/{id}
//    */
//   delete(id: string): Observable<ApiResponse<void>> {
//     return this.http.delete<ApiResponse<void>>(`${this.base}/${id}`);
//   }

//   /**
//    * PATCH /api/finance/invoiceitems/{id}/recompute
//    */
//   recompute(
//     id: string,
//     discountOverride?: number,
//   ): Observable<ApiResponse<InvoiceItemResponseDto>> {
//     let params = new HttpParams();
//     if (discountOverride != null) params = params.set('discountOverride', discountOverride);
//     return this.http.patch<ApiResponse<InvoiceItemResponseDto>>(
//       `${this.base}/${id}/recompute`,
//       {},
//       { params },
//     );
//   }
// }