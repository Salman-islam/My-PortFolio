import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
export interface Ticket { id: number; title: string; requester: string; priority: string; status: string; }
export type TicketInput = Omit<Ticket, 'id'>;
@Injectable({providedIn: 'root'})
export class ApiService {
  private http = inject(HttpClient);
  private url = '/api/tickets';
  list() { return this.http.get<Ticket[]>(this.url); }
  create(input: TicketInput) { return this.http.post<Ticket>(this.url, input); }
  update(id: number, input: TicketInput) { return this.http.put<Ticket>(`${this.url}/${id}`, input); }
  remove(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}
