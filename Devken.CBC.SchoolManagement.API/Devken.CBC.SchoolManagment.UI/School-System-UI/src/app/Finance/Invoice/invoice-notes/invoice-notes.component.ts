import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';

@Component({
  selector: 'app-invoice-notes',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatCardModule,
    FuseAlertComponent,
  ],
  templateUrl: './invoice-notes.component.html',
})
export class InvoiceNotesComponent implements OnInit, OnChanges {
  @Input() formData: any = '';
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<string>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.formValid.emit(true);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue({ notes: this.formData ?? '' }, { emitEvent: false });
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      notes: [this.formData ?? '', []],
    });
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit(value.notes);
      this.formValid.emit(true);
    });
  }
}