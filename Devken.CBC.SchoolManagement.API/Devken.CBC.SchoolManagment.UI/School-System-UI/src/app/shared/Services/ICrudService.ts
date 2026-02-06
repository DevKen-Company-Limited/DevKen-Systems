import { ApiResponse } from 'app/Tenant/types/school';
import { Observable } from 'rxjs';


export interface ICrudService<TCreate, TUpdate, TDto> {
  create(payload: TCreate): Observable<ApiResponse<TDto>>;
  update(id: string, payload: TUpdate): Observable<ApiResponse<TDto>>;
}