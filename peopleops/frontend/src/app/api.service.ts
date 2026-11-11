import { inject, Injectable, signal } from "@angular/core";
import { HttpClient, HttpInterceptorFn } from "@angular/common/http";
import { catchError, throwError } from "rxjs";
export interface User {
  id: number;
  name: string;
  email: string;
  role: string;
}
@Injectable({ providedIn: "root" })
export class Session {
  token = "";
  user = signal<User | null>(null);
  clear() {
    this.token = "";
    this.user.set(null);
  }
}
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(Session);
  return next(
    session.token
      ? req.clone({ setHeaders: { Authorization: `Bearer ${session.token}` } })
      : req,
  ).pipe(
    catchError((err) => {
      if (err.status === 401) session.clear();
      return throwError(() => err);
    }),
  );
};
@Injectable({ providedIn: "root" })
export class ApiService {
  private http = inject(HttpClient);
  session = inject(Session);
  get<T = any>(path: string) {
    return this.http.get<T>("/api/" + path);
  }
  post<T = any>(path: string, body: any) {
    return this.http.post<T>("/api/" + path, body);
  }
  put<T = any>(path: string, body: any) {
    return this.http.put<T>("/api/" + path, body);
  }
  delete(path: string) {
    return this.http.delete("/api/" + path);
  }
}
