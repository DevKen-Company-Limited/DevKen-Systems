import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { FeeItemResponseDto, CreateFeeItemDto, UpdateFeeItemDto } from 'app/finance/fee-item/Types/fee-item.model';


export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]>;
}
@Injectable({ providedIn: 'root' })
export class FeeItemService {
  private readonly apiBase = inject(API_BASE_URL);
  private readonly base    = `${this.apiBase}/api/finance/feeitems`;

  constructor(private http: HttpClient) {}

  getAll(filters?: {
    feeType?: number;
    applicableLevel?: number;
    isActive?: boolean;
  }): Observable<ApiResponse<FeeItemResponseDto[]>> {
    let params = new HttpParams();
    if (filters?.feeType !== undefined)
      params = params.set('feeType', filters.feeType.toString());
    if (filters?.applicableLevel !== undefined)
      params = params.set('applicableLevel', filters.applicableLevel.toString());
    if (filters?.isActive !== undefined)
      params = params.set('isActive', filters.isActive.toString());

    return this.http.get<ApiResponse<FeeItemResponseDto[]>>(this.base, { params });
  }

  getById(id: string): Observable<ApiResponse<FeeItemResponseDto>> {
    return this.http.get<ApiResponse<FeeItemResponseDto>>(`${this.base}/${id}`);
  }

  create(dto: CreateFeeItemDto): Observable<ApiResponse<FeeItemResponseDto>> {
    return this.http.post<ApiResponse<FeeItemResponseDto>>(this.base, dto);
  }

  update(id: string, dto: UpdateFeeItemDto): Observable<ApiResponse<FeeItemResponseDto>> {
    return this.http.put<ApiResponse<FeeItemResponseDto>>(`${this.base}/${id}`, dto);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.base}/${id}`);
  }

  toggleActive(id: string, isActive: boolean): Observable<ApiResponse<FeeItemResponseDto>> {
    return this.http.patch<ApiResponse<FeeItemResponseDto>>(
      `${this.base}/${id}/toggle-active`, isActive
    );
  }
}