import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
export type Status = 'Pending' | 'InProgress' | 'Done';
export interface User {
  id: number;
  name: string;
  email: string;
}
export interface WorkItem {
  id: number;
  title: string;
  userId: number;
  userName: string;
  status: Status;
  createdAt: string;
  additionalData: Record<string, unknown>;
}
@Injectable({ providedIn: 'root' })
export class Api {
  private http = inject(HttpClient);
  users() {
    return this.http.get<User[]>('/api/users');
  }
  createUser(data: { name: string; email: string }) {
    return this.http.post<User>('/api/users', data);
  }
  tasks(status: string, userId: string) {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    if (userId) params = params.set('userId', userId);
    return this.http.get<WorkItem[]>('/api/tasks', { params });
  }
  createTask(data: { title: string; userId: number; additionalData: Record<string, unknown> }) {
    return this.http.post<WorkItem>('/api/tasks', data);
  }
  changeStatus(id: number, status: Status) {
    return this.http.put<WorkItem>(`/api/tasks/${id}/status`, { status });
  }
}
export function errorMessage(error: HttpErrorResponse): string {
  if (error.status === 0)
    return 'No hay conexión con la API. Comprueba que el backend esté iniciado.';
  const errors = error.error?.errors;
  if (errors) return Object.values(errors).flat().join(' ');
  return (
    error.error?.detail ||
    error.error?.title ||
    'No se pudo completar la operación. Intenta nuevamente.'
  );
}
