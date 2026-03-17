import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from 'app/app.config';
import { ApiResponse } from 'app/Tenant/types/school';
import { ICrudService } from 'app/shared/Services/ICrudService';
import { IListService } from 'app/shared/Services/IListService';
import { CreateLibraryBranchRequest, UpdateLibraryBranchRequest, LibraryBranchDto } from 'app/Library/library-branch/Types/library-branch.types';


@Injectable({ providedIn: 'root' })
export class LibraryBranchService
  implements
    ICrudService<CreateLibraryBranchRequest, UpdateLibraryBranchRequest, LibraryBranchDto>,
    IListService<LibraryBranchDto> {

  private readonly _http       = inject(HttpClient);
  private readonly _apiBaseUrl = inject(API_BASE_URL);
  private readonly _base       = `${this._apiBaseUrl}/api/library/librarybranches`;

  getAll(schoolId?: string): Observable<ApiResponse<LibraryBranchDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<LibraryBranchDto[]>>(this._base, { params });
  }

  getById(id: string): Observable<ApiResponse<LibraryBranchDto>> {
    return this._http.get<ApiResponse<LibraryBranchDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateLibraryBranchRequest): Observable<ApiResponse<LibraryBranchDto>> {
    return this._http.post<ApiResponse<LibraryBranchDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateLibraryBranchRequest): Observable<ApiResponse<LibraryBranchDto>> {
    return this._http.put<ApiResponse<LibraryBranchDto>>(`${this._base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<any>> {
    return this._http.delete<ApiResponse<any>>(`${this._base}/${id}`);
  }
}