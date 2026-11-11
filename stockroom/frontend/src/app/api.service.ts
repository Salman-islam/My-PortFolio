import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
export interface Product { id: number; name: string; sku: string; category: string; quantity: number; }
export type ProductInput = Omit<Product, 'id'>;
@Injectable({providedIn: 'root'})
export class ApiService {
  private http = inject(HttpClient);
  private url = '/api/products';
  list() { return this.http.get<Product[]>(this.url); }
  create(input: ProductInput) { return this.http.post<Product>(this.url, input); }
  update(id: number, input: ProductInput) { return this.http.put<Product>(`${this.url}/${id}`, input); }
  remove(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}
