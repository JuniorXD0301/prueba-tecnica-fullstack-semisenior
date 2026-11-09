import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, finalize, of, startWith, Subject, switchMap } from 'rxjs';
import { Api, errorMessage, Status, User, WorkItem } from './api';
import { Message } from './message/message';

const emailWithCompleteDomain =
  /^[^\s@]+@(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$/;

@Component({
  selector: 'app-root',
  imports: [CommonModule, ReactiveFormsModule, Message],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private api = inject(Api);
  private fb = inject(FormBuilder);
  private destroyRef = inject(DestroyRef);
  private reload = new Subject<void>();
  users = signal<User[]>([]);
  tasks = signal<WorkItem[]>([]);
  readonly oldestFirst = signal(false);
  readonly orderedTasks = computed(() => {
    const tasks = this.tasks();
    // The API returns tasks in descending creation order.
    return this.oldestFirst() ? [...tasks].reverse() : tasks;
  });
  loading = signal(false);
  savingTask = signal(false);
  savingUser = signal(false);
  updating = signal<number[]>([]);
  error = signal('');
  notice = signal('');
  warning = signal('');
  status = '';
  userId = '';
  statuses: Status[] = ['Pending', 'InProgress', 'Done'];
  labels: Record<Status, string> = {
    Pending: 'Pendiente',
    InProgress: 'En progreso',
    Done: 'Terminada',
  };
  taskForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200), Validators.pattern(/\S/)]],
    userId: ['', Validators.required],
    priority: ['Medium'],
    estimatedCompletion: [''],
    tags: [''],
    metadata: ['{}'],
  });
  userForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120), Validators.pattern(/\S/)]],
    email: [
      '',
      [
        Validators.required,
        Validators.email,
        Validators.pattern(emailWithCompleteDomain),
        Validators.maxLength(254),
      ],
    ],
  });
  constructor() {
    this.loadUsers();
    this.reload
      .pipe(
        startWith(undefined),
        switchMap(() => {
          this.loading.set(true);
          this.error.set('');
          return this.api.tasks(this.status, this.userId).pipe(
            catchError((error) => {
              this.error.set(errorMessage(error));
              return of([] as WorkItem[]);
            }),
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((tasks) => this.tasks.set(tasks));
  }
  refresh() {
    this.reload.next();
  }
  filterStatus(value: string) {
    this.status = value;
    this.refresh();
  }
  priorityLabel(value: unknown): string {
    switch (value) {
      case 'Low': return 'Baja';
      case 'Medium': return 'Media';
      case 'High': return 'Alta';
      default: return '';
    }
  }
  filterUser(value: string) {
    this.userId = value;
    this.refresh();
  }
  loadUsers() {
    this.api
      .users()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => this.users.set(users),
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
  createUser() {
    if (this.savingUser()) return;
    this.clearMessages();
    this.userForm.markAllAsTouched();
    if (this.userForm.invalid) {
      this.warning.set('Completa el nombre y usa un correo con dominio completo, como nombre@ejemplo.com.');
      return;
    }
    this.savingUser.set(true);
    this.api
      .createUser(this.userForm.getRawValue())
      .pipe(
        finalize(() => this.savingUser.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (user) => {
          this.users.update((users) =>
            [...users, user].sort((a, b) => a.name.localeCompare(b.name)),
          );
          this.userForm.reset();
          this.notice.set('Usuario creado. Ya puedes asignarle tareas.');
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
  createTask() {
    if (this.savingTask()) return;
    this.clearMessages();
    this.taskForm.markAllAsTouched();
    if (this.taskForm.invalid) {
      this.warning.set('Completa el título y selecciona un responsable antes de crear la tarea.');
      return;
    }
    const data = this.taskForm.getRawValue();
    let metadata: unknown;
    try {
      metadata = JSON.parse(data.metadata);
      if (!metadata || typeof metadata !== 'object' || Array.isArray(metadata)) throw new Error();
    } catch {
      this.warning.set(
        'Los metadatos deben ser un objeto JSON válido, por ejemplo: {"equipo":"ventas"}.',
      );
      return;
    }
    this.savingTask.set(true);
    const additionalData: Record<string, unknown> = {
      priority: data.priority,
      tags: data.tags
        .split(',')
        .map((tag) => tag.trim())
        .filter(Boolean),
      metadata,
    };
    if (data.estimatedCompletion) additionalData['estimatedCompletion'] = data.estimatedCompletion;
    this.api
      .createTask({ title: data.title.trim(), userId: Number(data.userId), additionalData })
      .pipe(
        finalize(() => this.savingTask.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.taskForm.reset({ priority: 'Medium', metadata: '{}' });
          this.refresh();
          this.notice.set('Tarea creada en estado pendiente.');
        },
        error: (error) => this.error.set(errorMessage(error)),
      });
  }
  changeStatus(task: WorkItem, value: string) {
    if (task.status === value || this.updating().includes(task.id)) return;
    this.clearMessages();
    this.updating.update((ids) => [...ids, task.id]);
    this.api
      .changeStatus(task.id, value as Status)
      .pipe(
        finalize(() => this.updating.update((ids) => ids.filter((id) => id !== task.id))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.refresh();
          this.notice.set('Estado actualizado.');
        },
        error: (error) => {
          this.refresh();
          this.error.set(errorMessage(error));
        },
      });
  }

  private clearMessages() {
    this.error.set('');
    this.notice.set('');
    this.warning.set('');
  }
}
