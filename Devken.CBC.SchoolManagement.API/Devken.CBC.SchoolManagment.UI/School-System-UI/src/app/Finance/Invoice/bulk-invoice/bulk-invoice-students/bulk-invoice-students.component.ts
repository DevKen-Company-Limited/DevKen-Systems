import {
  Component, Input, Output, EventEmitter,
  OnChanges, SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BulkInvoiceStudentRow } from '../../Types/bulk-invoice.types';

@Component({
  selector: 'app-bulk-invoice-students',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatCheckboxModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './bulk-invoice-students.component.html',
})
export class BulkInvoiceStudentsComponent implements OnChanges {
  @Input() students: BulkInvoiceStudentRow[] = [];
  @Input() isLoading = false;

  @Output() studentsChanged = new EventEmitter<BulkInvoiceStudentRow[]>();
  @Output() formValid = new EventEmitter<boolean>();

  searchQuery = '';
  filteredStudents: BulkInvoiceStudentRow[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['students']) {
      this.filteredStudents = [...this.students];
      this.emitValid();
    }
  }

  get selectedCount(): number {
    return this.students.filter(s => s.selected).length;
  }

  get allSelected(): boolean {
    return this.students.length > 0 && this.students.every(s => s.selected);
  }

  get someSelected(): boolean {
    return this.students.some(s => s.selected);
  }

  toggle(student: BulkInvoiceStudentRow): void {
    student.selected = !student.selected;
    this.studentsChanged.emit([...this.students]);
    this.emitValid();
  }

  toggleAll(checked: boolean): void {
    this.students.forEach(s => (s.selected = checked));
    this.filteredStudents.forEach(s => (s.selected = checked));
    this.studentsChanged.emit([...this.students]);
    this.emitValid();
  }

  onSearch(): void {
    const q = this.searchQuery.toLowerCase().trim();
    if (!q) {
      this.filteredStudents = [...this.students];
    } else {
      this.filteredStudents = this.students.filter(s =>
        s.studentName.toLowerCase().includes(q) ||
        (s.admissionNumber ?? '').toLowerCase().includes(q)
      );
    }
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase();
  }

  private emitValid(): void {
    this.formValid.emit(this.selectedCount > 0);
  }
}