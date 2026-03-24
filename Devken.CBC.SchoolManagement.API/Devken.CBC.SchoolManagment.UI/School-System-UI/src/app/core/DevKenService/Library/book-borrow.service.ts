import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from 'app/Tenant/types/school';
import { BookBorrowDto, BookBorrowItemDto, CreateBookBorrowRequest, ReturnBookRequest, ReturnMultipleBooksRequest, UpdateBookBorrowRequest } from 'app/Library/book-borrow/Types/book-borrow.types';



@Injectable({ providedIn: 'root' })
export class BookBorrowService {
  private readonly _base = 'api/library/borrows';

  constructor(private _http: HttpClient) {}

  // ── Standard CRUD ─────────────────────────────────────────────────────────

  getAll(schoolId?: string): Observable<ApiResponse<BookBorrowDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookBorrowDto[]>>(this._base, { params });
  }

  getById(id: string): Observable<ApiResponse<BookBorrowDto>> {
    return this._http.get<ApiResponse<BookBorrowDto>>(`${this._base}/${id}`);
  }

  create(payload: CreateBookBorrowRequest): Observable<ApiResponse<BookBorrowDto>> {
    return this._http.post<ApiResponse<BookBorrowDto>>(this._base, payload);
  }

  update(id: string, payload: UpdateBookBorrowRequest): Observable<ApiResponse<BookBorrowDto>> {
    return this._http.put<ApiResponse<BookBorrowDto>>(`${this._base}/${id}`, payload);
  }

  delete(id: string): Observable<ApiResponse<void>> {
    return this._http.delete<ApiResponse<void>>(`${this._base}/${id}`);
  }

  // ── Filters ───────────────────────────────────────────────────────────────

  getByMember(memberId: string): Observable<ApiResponse<BookBorrowDto[]>> {
    return this._http.get<ApiResponse<BookBorrowDto[]>>(`${this._base}/member/${memberId}`);
  }

  getActive(schoolId?: string): Observable<ApiResponse<BookBorrowDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookBorrowDto[]>>(`${this._base}/active`, { params });
  }

  getOverdue(schoolId?: string): Observable<ApiResponse<BookBorrowDto[]>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.get<ApiResponse<BookBorrowDto[]>>(`${this._base}/overdue`, { params });
  }

  // ── Return ────────────────────────────────────────────────────────────────

  returnBook(payload: ReturnBookRequest): Observable<ApiResponse<BookBorrowItemDto>> {
    return this._http.post<ApiResponse<BookBorrowItemDto>>(`${this._base}/return`, payload);
  }

  returnMultipleBooks(payload: ReturnMultipleBooksRequest): Observable<ApiResponse<BookBorrowItemDto[]>> {
    return this._http.post<ApiResponse<BookBorrowItemDto[]>>(`${this._base}/return/multiple`, payload);
  }

  // ── Eligibility ───────────────────────────────────────────────────────────

  canMemberBorrow(memberId: string): Observable<ApiResponse<{ canBorrow: boolean }>> {
    return this._http.get<ApiResponse<{ canBorrow: boolean }>>(`${this._base}/member/${memberId}/can-borrow`);
  }

  getActiveBorrowCount(memberId: string): Observable<ApiResponse<{ activeBorrowCount: number }>> {
    return this._http.get<ApiResponse<{ activeBorrowCount: number }>>(`${this._base}/member/${memberId}/active-count`);
  }

  // ── Admin ─────────────────────────────────────────────────────────────────

  processOverdue(schoolId?: string): Observable<ApiResponse<void>> {
    let params = new HttpParams();
    if (schoolId) params = params.set('schoolId', schoolId);
    return this._http.post<ApiResponse<void>>(`${this._base}/process-overdue`, {}, { params });
  }
}