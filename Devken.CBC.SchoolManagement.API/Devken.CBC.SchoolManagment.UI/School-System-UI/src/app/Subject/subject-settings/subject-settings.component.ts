// subject-settings/subject-settings.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';

@Component({
  selector: 'app-subject-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSlideToggleModule,
    MatIconModule,
    MatCardModule,
    FuseAlertComponent,
  ],
  templateUrl: './subject-settings.component.html',
})
export class SubjectSettingsComponent implements OnInit, OnChanges {

  @Input() formData: any = {};
  @Input() isEditMode = false;
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  form!: FormGroup;

  ngOnInit(): void {
    this.form = this.fb.group({
     isActive:     [this.formData?.isActive     ?? true],
    });

    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit(value);
      this.formValid.emit(true); // Settings are always valid (no required fields)
    });

    this.formValid.emit(true);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) {
      this.form.patchValue({
       isActive:     this.formData?.isActive     ?? true,
      }, { emitEvent: false });
    }
  }

  get isActive():     boolean { return !!this.form?.get('isActive')?.value; }
}