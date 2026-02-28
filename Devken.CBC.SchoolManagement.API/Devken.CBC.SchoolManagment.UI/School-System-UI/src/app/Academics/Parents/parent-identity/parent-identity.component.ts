import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';

@Component({
  selector: 'app-parent-identity',
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
  templateUrl: './parent-identity.component.html',
})
export class ParentIdentityComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<any>();
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
      this.form.patchValue({
        nationalIdNumber: this.formData?.nationalIdNumber ?? '',
        passportNumber:   this.formData?.passportNumber   ?? '',
      }, { emitEvent: false });
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      nationalIdNumber: [this.formData?.nationalIdNumber ?? '', []],
      passportNumber:   [this.formData?.passportNumber   ?? '', []],
    });
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit(value);
      this.formValid.emit(true);
    });
  }
}