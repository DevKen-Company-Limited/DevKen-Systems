// steps/student-location/student-location.component.ts
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-student-location',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './Student location.component.html',
  styleUrls: ['../../../shared/scss/shared-step.scss'],
})
export class StudentLocationComponent implements OnInit, OnChanges {
  @Input() formData: any = {};
  @Output() formChanged = new EventEmitter<any>();
  @Output() formValid   = new EventEmitter<boolean>();

  form!: FormGroup;

  counties = [
    'Nairobi','Mombasa','Kisumu','Nakuru','Uasin Gishu','Kiambu','Machakos',
    'Nyeri','Meru','Kilifi','Kakamega','Bungoma','Migori','Homa Bay','Kisii',
    'Siaya','Nyamira','Bomet','Kericho','Nandi','Laikipia','Samburu','Isiolo',
    'Marsabit','Wajir','Mandera','Garissa','Tana River','Lamu','Kwale','Taita Taveta',
    'Kajiado','Makueni','Kitui','Embu','Tharaka Nithi','Kirinyaga','Murang\'a',
    'Nyandarua','Trans Nzoia','West Pokot','Elgeyo Marakwet','Baringo','Turkana',
    'Narok','Vihiga','Butere','Busia','Turkana',
  ].filter((v, i, a) => a.indexOf(v) === i).sort();

  constructor(private fb: FormBuilder) {}

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