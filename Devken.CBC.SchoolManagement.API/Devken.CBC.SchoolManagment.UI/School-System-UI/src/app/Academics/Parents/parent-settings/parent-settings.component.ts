import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';

@Component({
  selector: 'app-parent-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSlideToggleModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatCardModule,
    FuseAlertComponent,
  ],
  templateUrl: './parent-settings.component.html',
})
export class ParentSettingsComponent implements OnInit, OnChanges {
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
        isPrimaryContact:   this.formData?.isPrimaryContact   ?? true,
        isEmergencyContact: this.formData?.isEmergencyContact ?? true,
        hasPortalAccess:    this.formData?.hasPortalAccess    ?? false,
        portalUserId:       this.formData?.portalUserId       ?? '',
      }, { emitEvent: false });
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      isPrimaryContact:   [this.formData?.isPrimaryContact   ?? true],
      isEmergencyContact: [this.formData?.isEmergencyContact ?? true],
      hasPortalAccess:    [this.formData?.hasPortalAccess    ?? false],
      portalUserId:       [this.formData?.portalUserId       ?? ''],
    });
  }

  private setupListeners(): void {
    this.form.valueChanges.subscribe(value => {
      this.formChanged.emit(value);
      this.formValid.emit(true);
    });
  }

  get isPrimary():   boolean { return !!this.form?.get('isPrimaryContact')?.value; }
  get isEmergency(): boolean { return !!this.form?.get('isEmergencyContact')?.value; }
  get hasPortal():   boolean { return !!this.form?.get('hasPortalAccess')?.value; }
}