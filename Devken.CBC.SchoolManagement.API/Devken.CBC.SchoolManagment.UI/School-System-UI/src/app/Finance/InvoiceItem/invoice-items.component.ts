import { Component, Input, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { PageHeaderComponent, Breadcrumb } from 'app/shared/Page-Header/page-header.component';
import { InvoiceItemFormComponent } from './form/invoice-item-form.component';
import { InvoiceItemsListComponent } from './list/invoice-items-list.component';
import { PanelMode, InvoiceItemResponseDto } from './Types/invoice-item.types';


@Component({
  selector: 'app-invoice-items',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    PageHeaderComponent,
    InvoiceItemsListComponent,
    InvoiceItemFormComponent,
  ],
  templateUrl: './invoice-items.component.html',
})
export class InvoiceItemsComponent {
  // ── Inputs ────────────────────────────────────────────────────────────────
  @Input({ required: true }) invoiceId!: string;
  @Input() invoiceNumber?: string;

  // ── Child ref (to call loadItems after save) ──────────────────────────────
  @ViewChild(InvoiceItemsListComponent) listRef!: InvoiceItemsListComponent;

  // ── Panel state ───────────────────────────────────────────────────────────
  panelMode: PanelMode   = null;
  editingItem?: InvoiceItemResponseDto;
  showFilters = false;

  // ── Breadcrumbs ───────────────────────────────────────────────────────────
  breadcrumbs: Breadcrumb[] = [
    { label: 'Finance' },
    { label: 'Invoices', url: '/finance/invoices' },
    { label: 'Line Items' },
  ];

  // ── Panel open/close ──────────────────────────────────────────────────────

  openCreate(): void {
    this.editingItem = undefined;
    this.panelMode   = 'create';
  }

  openEdit(item: InvoiceItemResponseDto): void {
    this.editingItem = item;
    this.panelMode   = 'edit';
  }

  closePanel(): void {
    this.panelMode   = null;
    this.editingItem = undefined;
  }

  // ── After form save ───────────────────────────────────────────────────────

  onSaved(_item: InvoiceItemResponseDto): void {
    this.closePanel();
    this.listRef?.loadItems();
  }
}