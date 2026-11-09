import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-message',
  templateUrl: './message.html',
  styleUrl: './message.css',
})
export class Message {
  text = input.required<string>();
  severity = input<'success' | 'warning' | 'error'>('success');
  title = computed(() => ({
    success: 'Operación exitosa',
    warning: 'Revisa la información',
    error: 'No se pudo completar la operación',
  })[this.severity()]);
  dismissed = output<void>();
}
