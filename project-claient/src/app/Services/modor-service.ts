import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ModorService {
  private http = inject(HttpClient);
  // שינוי הפורט ל-5000 והפנייה לקונטרולר של התורמים (Donors/Modor)
  private readonly apiUrl = 'http://localhost:5000/api/Donor';
  public refreshList$ = new Subject<void>();

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  add(m: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, m);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  update(g: any): Observable<any> {
    const id = g.id || g.Id;
    return this.http.put<any>(`${this.apiUrl}/${id}`, g);
  }
}