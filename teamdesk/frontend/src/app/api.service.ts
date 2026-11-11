import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
export interface Employee { id: number; name: string; email: string; department: string; status: string; }
export type EmployeeInput = Omit<Employee, 'id'>;
@Injectable({providedIn: 'root'})
export class ApiService {
  private http = inject(HttpClient);
  private url = '/api/employees';
  list() { return this.http.get<Employee[]>(this.url); }
  create(input: EmployeeInput) { return this.http.post<Employee>(this.url, input); }
  update(id: number, input: EmployeeInput) { return this.http.put<Employee>(`${this.url}/${id}`, input); }
  remove(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}
