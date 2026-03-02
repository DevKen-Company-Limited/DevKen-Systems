import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import { CreateInvoiceItemDto, InvoiceItemResponseDto, UpdateInvoiceItemDto } from 'app/Finance/InvoiceItem/Types/invoice-item.types';


@Injectable({ providedIn: 'root' })
export class InvoiceItemService {
  private readonly apiBase = inject(API_BASE_URL);

  private baseUrl(invoiceId: string): string {
    return `${this.apiBase}/api/invoices/${invoiceId}/items`;
  }

  constructor(private http: HttpClient) {}

  // ── Query ─────────────────────────────────────────────────────────────────

  getByInvoice(
    invoiceId: string
  ): Observable<ApiResponse<InvoiceItemResponseDto[]>> {
    return this.http.get<ApiResponse<InvoiceItemResponseDto[]>>(
      this.baseUrl(invoiceId)
    );
  }

  getById(
    invoiceId: string,
    id: string
  ): Observable<ApiResponse<InvoiceItemResponseDto>> {
    return this.http.get<ApiResponse<InvoiceItemResponseDto>>(
      `${this.baseUrl(invoiceId)}/${id}`
    );
  }

  delete(invoiceId: string, id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.baseUrl(invoiceId)}/${id}`);
  }

  // ── ICrudService ──────────────────────────────────────────────────────────

  create(
    payload: CreateInvoiceItemDto
  ): Observable<ApiResponse<InvoiceItemResponseDto>> {
    return this.http.post<ApiResponse<InvoiceItemResponseDto>>(
      this.baseUrl(payload.invoiceId),
      payload
    );
  }

  update(
    id: string,
    payload: UpdateInvoiceItemDto,
    invoiceId: string
  ): Observable<ApiResponse<InvoiceItemResponseDto>> {
    return this.http.put<ApiResponse<InvoiceItemResponseDto>>(
      `${this.baseUrl(invoiceId)}/${id}`,
      payload
    );
  }

  recompute(
    invoiceId: string,
    id: string,
    discountOverride?: number
  ): Observable<ApiResponse<InvoiceItemResponseDto>> {
    const params = discountOverride != null
      ? `?discountOverride=${discountOverride}`
      : '';
    return this.http.patch<ApiResponse<InvoiceItemResponseDto>>(
      `${this.baseUrl(invoiceId)}/${id}/recompute${params}`,
      {}
    );
  }
}