import { Component, Inject, OnInit, AfterViewInit, ViewChild, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CreateParentDto, UpdateParentDto, ParentDialogData, ParentRelationship } from 'app/Academics/Parents/Types/Parent.types';
import { ParentService } from 'app/core/DevKenService/Parents/Parent.service';
import { BaseFormDialog } from 'app/shared/dialogs/BaseFormDialog';
import { FormDialogComponent, DialogHeader, DialogTab, DialogFooter } from 'app/shared/dialogs/form/form-dialog.component';

@Component({
  selector: 'app-parent-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    FormDialogComponent,
  ],
  templateUrl: './parent-form-dialog.component.html',
})
export class ParentFormDialogComponent
  extends BaseFormDialog<CreateParentDto, UpdateParentDto, any, ParentDialogData>
  implements OnInit, AfterViewInit
{
  @ViewChild('basicTab')      basicTab!: TemplateRef<any>;
  @ViewChild('contactTab')    contactTab!: TemplateRef<any>;
  @ViewChild('identityTab')   identityTab!: TemplateRef<any>;
  @ViewChild('employmentTab') employmentTab!: TemplateRef<any>;
  @ViewChild('settingsTab')   settingsTab!: TemplateRef<any>;

  formSubmitted = false;
  activeTab = 0;
  tabTemplates: { [tabId: string]: TemplateRef<any> } = {};

  relationships = [
    { label: 'Father',      value: ParentRelationship.Father },
    { label: 'Mother',      value: ParentRelationship.Mother },
    { label: 'Guardian',    value: ParentRelationship.Guardian },
    { label: 'Sibling',     value: ParentRelationship.Sibling },
    { label: 'Grandparent', value: ParentRelationship.Grandparent },
    { label: 'Uncle',       value: ParentRelationship.Uncle },
    { label: 'Aunt',        value: ParentRelationship.Aunt },
    { label: 'Other',       value: ParentRelationship.Other },
  ];

  dialogHeader: DialogHeader = {
    title: this.data.mode === 'create' ? 'Add Parent / Guardian' : 'Edit Parent / Guardian',
    subtitle: this.data.mode === 'create' ? 'Fill in the details below' : `Editing: ${this.data.parent?.fullName}`,
    icon: 'family_restroom',
    gradient: 'bg-gradient-to-r from-teal-600 via-emerald-600 to-green-700',
  };

  tabs: DialogTab[] = [
    { id: 'basic',      label: 'Basic Info',  icon: 'person',   fields: ['firstName', 'lastName', 'relationship'] },
    { id: 'contact',    label: 'Contact',     icon: 'contacts', fields: ['phoneNumber', 'email', 'address'] },
    { id: 'identity',   label: 'Identity',    icon: 'badge',    fields: [] },
    { id: 'employment', label: 'Employment',  icon: 'work',     fields: [] },
    { id: 'settings',   label: 'Settings',    icon: 'settings', fields: [] },
  ];

  get footerConfig(): DialogFooter {
    return {
      cancelText: 'Cancel',
      submitText: this.data.mode === 'create' ? 'Save Parent' : 'Update Parent',
      submitIcon: 'save',
      loading: this.isSaving,
      loadingText: 'Saving...',
      showError: true,
      errorMessage: 'Please fix validation errors before saving.',
    };
  }

  constructor(
    fb: FormBuilder,
    parentService: ParentService,
    snackBar: MatSnackBar,
    dialogRef: MatDialogRef<ParentFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public override data: ParentDialogData,
  ) {
    super(fb, parentService, snackBar, dialogRef, data);
  }

  ngOnInit(): void {
    this.init();
  }

  ngAfterViewInit(): void {
    this.tabTemplates = {
      basic:      this.basicTab,
      contact:    this.contactTab,
      identity:   this.identityTab,
      employment: this.employmentTab,
      settings:   this.settingsTab,
    };
  }

  protected buildForm(): FormGroup {
    return this.fb.group({
      firstName:               ['', [Validators.required, Validators.maxLength(100)]],
      middleName:              ['', Validators.maxLength(100)],
      lastName:                ['', [Validators.required, Validators.maxLength(100)]],
      phoneNumber:             ['', Validators.maxLength(20)],
      alternativePhoneNumber:  ['', Validators.maxLength(20)],
      email:                   ['', [Validators.email, Validators.maxLength(150)]],
      address:                 ['', Validators.maxLength(500)],
      nationalIdNumber:        ['', Validators.maxLength(20)],
      passportNumber:          ['', Validators.maxLength(20)],
      occupation:              ['', Validators.maxLength(100)],
      employer:                ['', Validators.maxLength(150)],
      employerContact:         ['', Validators.maxLength(100)],
      relationship:            [ParentRelationship.Father, Validators.required],
      isPrimaryContact:        [true],
      isEmergencyContact:      [true],
      hasPortalAccess:         [false],
      portalUserId:            ['', Validators.maxLength(256)],
    });
  }

  protected override patchForEdit(item: any): void {
    this.form.patchValue({
      firstName:              item.firstName,
      middleName:             item.middleName || '',
      lastName:               item.lastName,
      phoneNumber:            item.phoneNumber || '',
      alternativePhoneNumber: item.alternativePhoneNumber || '',
      email:                  item.email || '',
      address:                item.address || '',
      nationalIdNumber:       item.nationalIdNumber || '',
      passportNumber:         item.passportNumber || '',
      occupation:             item.occupation || '',
      employer:               item.employer || '',
      employerContact:        item.employerContact || '',
      relationship:           item.relationship,
      isPrimaryContact:       item.isPrimaryContact,
      isEmergencyContact:     item.isEmergencyContact,
      hasPortalAccess:        item.hasPortalAccess,
      portalUserId:           item.portalUserId || '',
    });
  }

  onSubmit(): void {
    this.formSubmitted = true;
    this.save(
      (raw) => raw as CreateParentDto,
      (raw) => raw as UpdateParentDto,
      () => this.data.parent!.id,
    );
  }

  onCancel(): void {
    this.close();
  }

  onTabChange(index: number): void {
    this.activeTab = index;
  }
}