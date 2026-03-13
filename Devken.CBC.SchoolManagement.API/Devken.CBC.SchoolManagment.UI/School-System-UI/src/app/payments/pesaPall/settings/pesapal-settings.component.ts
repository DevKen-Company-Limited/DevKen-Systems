// ═══════════════════════════════════════════════════════════════════
// pesapal-settings.component.ts
// Place in: src/app/modules/finance/pesapal/settings/
// ═══════════════════════════════════════════════════════════════════

import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';

import { PesaPalSettingsService } from '../../../core/DevKenService/payments/pesapal-settings.service';
import { PesaPalSettingsDto, PesaPalEnvironment } from '../pesapal.types';

/** Sentinel value the server returns for the masked secret. */
const MASKED = '••••••••';

@Component({
  selector: 'app-pesapal-settings',
  standalone: true,
  templateUrl: './pesapal-settings.component.html',
  styleUrls:   ['./pesapal-settings.component.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
  ],
})
export class PesaPalSettingsComponent implements OnInit {

  private readonly _fb    = inject(FormBuilder);
  private readonly _svc   = inject(PesaPalSettingsService);
  private readonly _snack = inject(MatSnackBar);

  form!: FormGroup;

  loading        = true;
  saving         = false;
  registeringIpn = false;
  showKey        = false;
  showSecret     = false;
  ipnRegistered  = false;
  ipnId: string | null = null;

  private readonly _httpsPattern = /^https?:\/\/.+/;
  private _originalValues: Partial<PesaPalSettingsDto> = {};

  get isSandbox(): boolean {
    return this.form?.get('environment')?.value === PesaPalEnvironment.Sandbox;
  }

  ngOnInit(): void {
    this._buildForm();
    this._load();
  }

  onEnvironmentChange(): void {
    this.form.patchValue({
      baseUrl: this.isSandbox
        ? 'https://cybqa.pesapal.com/pesapalv3'
        : 'https://pay.pesapal.com/v3',
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving = true;

    const raw = this.form.getRawValue();

    // If the secret field still holds the masked sentinel, send empty string
    // so the backend knows to keep the existing stored secret unchanged.
    const dto: PesaPalSettingsDto = {
      ...raw,
      consumerSecret: raw.consumerSecret === MASKED ? '' : raw.consumerSecret,
    };

    this._svc.saveSettings(dto).subscribe({
      next: () => {
        this.saving = false;
        this.form.markAsPristine();
        // Restore the mask so the field doesn't appear blank after save
        this.form.get('consumerSecret')!.setValue(MASKED, { emitEvent: false });
        this._originalValues = this.form.getRawValue();
        this._snack.open('PesaPal settings saved.', 'OK', { duration: 3000 });
      },
      error: (err) => {
        this.saving = false;
        this._snack.open(
          err?.error?.message ?? 'Failed to save settings.',
          'Dismiss',
          { duration: 5000 }
        );
      },
    });
  }

  registerIpn(): void {
    this.registeringIpn = true;
    this._svc.registerIpn().subscribe({
      next: (res) => {
        this.registeringIpn = false;
        this.ipnRegistered  = true;
        this.ipnId          = res.ipnId;
        this._snack.open('IPN registered successfully.', 'OK', { duration: 3000 });
      },
      error: (err) => {
        this.registeringIpn = false;
        this._snack.open(
          err?.error?.message ?? 'IPN registration failed.',
          'Dismiss',
          { duration: 5000 }
        );
      },
    });
  }

  reset(): void {
    this.form.patchValue(this._originalValues);
    this.form.markAsPristine();
  }

  // ── Private ─────────────────────────────────────────────────────

  private _buildForm(): void {
    this.form = this._fb.group({
      environment: [PesaPalEnvironment.Sandbox],

      // Required — populated from the server's real value on load
      consumerKey: ['', Validators.required],

      // NOT required — server returns "••••••••" (masked).
      // Only needs to be filled when the user wants to change the secret.
      consumerSecret: [''],

      baseUrl: [
        'https://cybqa.pesapal.com/pesapalv3',
        [Validators.required, Validators.pattern(this._httpsPattern)],
      ],
      ipnUrl: [
        '',
        [Validators.required, Validators.pattern(this._httpsPattern)],
      ],
      callbackUrl: [
        '',
        [Validators.required, Validators.pattern(this._httpsPattern)],
      ],
    });
  }

  private _load(): void {
    this._svc.getSettings().subscribe({
      next: (dto) => {
        this.loading = false;

        // Map environment string → enum for the slide-toggle
        const env =
          dto.environment === 'Production'
            ? PesaPalEnvironment.Production
            : PesaPalEnvironment.Sandbox;

        // Populate every field from the server response.
        // consumerSecret comes back as "••••••••" — show that as a placeholder
        // so it's clear a secret is already stored, without revealing the value.
        this.form.patchValue({
          environment:    env,
          consumerKey:    dto.consumerKey    ?? '',
          consumerSecret: dto.consumerSecret ?? MASKED,
          baseUrl:        dto.baseUrl        ?? '',
          ipnUrl:         dto.ipnUrl         ?? '',
          callbackUrl:    dto.callbackUrl    ?? '',
        });

        this.form.markAsPristine();
        this._originalValues = this.form.getRawValue();

        this.ipnRegistered = dto.ipnRegistered ?? false;
        this.ipnId         = dto.ipnId         ?? null;
      },
      error: () => {
        this.loading = false;
        this._snack.open(
          'Could not load PesaPal settings.',
          'Dismiss',
          { duration: 5000 }
        );
      },
    });
  }
}