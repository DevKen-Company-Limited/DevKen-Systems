import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { FuseAlertComponent } from '@fuse/components/alert';

@Component({
  selector: 'app-student-location',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatCardModule,
    FuseAlertComponent,
  ],
  templateUrl: './student-location.component.html',
})
export class StudentLocationComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);

  form!: FormGroup;

  counties = [
    'Nairobi','Mombasa','Kisumu','Nakuru','Uasin Gishu','Kiambu','Machakos',
    'Nyeri','Meru','Kilifi','Kakamega','Bungoma','Migori','Homa Bay','Kisii',
    'Siaya','Nyamira','Bomet','Kericho','Nandi','Laikipia','Samburu','Isiolo',
    'Marsabit','Wajir','Mandera','Garissa','Tana River','Lamu','Kwale','Taita Taveta',
    'Kajiado','Makueni','Kitui','Embu','Tharaka Nithi','Kirinyaga','Murang\'a',
    'Nyandarua','Trans Nzoia','West Pokot','Elgeyo Marakwet','Baringo','Turkana',
    'Narok','Vihiga','Busia',
  ].filter((v, i, a) => a.indexOf(v) === i).sort();

  ngOnInit(): void {
    this.form = this.fb.group({
      placeOfBirth: [this.formData?.placeOfBirth ?? ''],
      county:       [this.formData?.county       ?? ''],
      subCounty:    [this.formData?.subCounty    ?? ''],
      homeAddress:  [this.formData?.homeAddress  ?? ''],
    });
    this.form.valueChanges.subscribe(v => {
      this.formChanged.emit(v);
      this.formValid.emit(true); // location is all optional
    });
    this.formValid.emit(true);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['formData'] && this.form) this.form.patchValue(this.formData, { emitEvent: false });
  }
}