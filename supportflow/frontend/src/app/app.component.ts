import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, Ticket, TicketInput } from './api.service';
@Component({selector:'app-root', standalone:true, imports:[CommonModule, FormsModule], templateUrl:'./app.component.html'})
export class AppComponent implements OnInit {
  private api = inject(ApiService);
  items: Ticket[] = []; search = ''; filter = ''; loading = false; saving = false;
  error = ''; notice = ''; showForm = false; editId: number | null = null;
  options = ["High", "Medium", "Low"]; statuses = ["Open", "In progress", "Resolved"];
  form: TicketInput = {"title": "", "requester": "", "priority": "High", "status": "Open"};
  ngOnInit() { this.load(); }
  get filtered() { const q = this.search.trim().toLowerCase(); return this.items.filter(x => (!this.filter || x.priority === this.filter) && Object.values(x).join(' ').toLowerCase().includes(q)); }
  get metric() { return this.items.filter(x => x.status === 'Open').length; }
  get metric2() { return new Set(this.items.map(x => x.priority)).size; }
  load() { this.loading = true; this.error = ''; this.api.list().subscribe({next: x => {this.items = x; this.loading = false;}, error: () => {this.loading = false; this.error = 'Could not connect to the API. Start the backend on port 5080, then retry.';}}); }
  open(item?: Ticket) { this.error = ''; this.notice = ''; this.editId = item?.id ?? null; this.form = item ? {title: item.title, requester: item.requester, priority: item.priority, status: item.status} : {"title": "", "requester": "", "priority": "High", "status": "Open"}; this.showForm = true; }
  save() { if (this.saving) return; this.saving = true; this.error = ''; const request = this.editId !== null ? this.api.update(this.editId, this.form) : this.api.create(this.form); request.subscribe({next: () => {this.saving = false; this.showForm = false; this.notice = 'Saved successfully.'; this.load();}, error: err => {this.saving = false; this.error = err.error?.errors ? Object.values(err.error.errors).flat().join(' ') : 'Save failed. Check the API connection and try again.';}}); }
  remove(item: Ticket) { if (!confirm('Delete this record? This cannot be undone.')) return; this.error = ''; this.api.remove(item.id).subscribe({next: () => {this.notice = 'Record deleted.'; this.load();}, error: () => this.error = 'Delete failed. Please refresh and try again.'}); }
}
