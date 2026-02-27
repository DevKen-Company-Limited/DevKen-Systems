import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';

@Component({
  selector: 'app-parent-contact',
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
  templateUrl: './parent-contact.component.html',
})
export class ParentContactComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  ngOnInit(): void {
    this.buildForm();
    this.setupListeners();
    this.formValid.emit(true); // all fields optional
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue({
        phoneNumber:           this.formData?.phoneNumber           ?? '',
        alternativePhoneNumber: this.formData?.alternativePhoneNumber ?? '',
        email:                 this.formData?.email                 ?? '',
        address:               this.formData?.address               ?? '',
      }, { emitEvent: false });
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      phoneNumber:           [this.formData?.phoneNumber           ?? '', []],
      alternativePhoneNumber: [this.formData?.alternativePhoneNumber ?? '', []],
      email:                 [this.formData?.email                 ?? '', []],
      address:               [this.formData?.address               ?? '', []],
    });
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit(value);
      this.formValid.emit(true);
    });
  }

  isInvalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c && c.invalid && (c.dirty || c.touched));
  }

  getError(field: string): string {
    const c = this.form.get(field);
    if (!c?.errors) return '';
    if (c.errors['email']) return 'Invalid email address';
    return 'Invalid value';
  }
}