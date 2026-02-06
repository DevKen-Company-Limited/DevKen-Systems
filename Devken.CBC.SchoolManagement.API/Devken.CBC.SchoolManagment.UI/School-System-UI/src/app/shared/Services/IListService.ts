import { ApiResponse } from 'app/Tenant/types/school';
import { Observable } from 'rxjs';


export interface IListService<TDto> {
  getAll(): Observable<ApiResponse<TDto[]>>;
  delete(id: string): Observable<ApiResponse<any>>;
}