import { Component, Inject, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { SchoolService } from 'app/core/DevKenService/Tenant/SchoolService';
import { SchoolWithSubscription, SubscriptionStatus, BillingCycle } from 'app/Tenant/types/school';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatOptionModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { forkJoin } from 'rxjs';
import { EnumOption, EnumService } from 'app/core/DevKenService/Tenant/EnumService';
import { SubscriptionPlanDto, SubscriptionPlanService } from 'app/core/DevKenService/Tenant/SubscriptionPlanService';
import { MpesaPaymentDialogComponent } from '../payments/mpesa-payment-dialog.component';

@Component({
  selector: 'app-manage-subscription-dialog',
  standalone: true,
  templateUrl: './manage-subscription-dialog.component.html',
  styleUrls: ['./manage-subscription-dialog.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatOptionModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatSnackBarModule
  ],
  providers: [DatePipe]
})
export class ManageSubscriptionDialogComponent implements OnInit {
  isLoading = false;

  subscriptionPlans: SubscriptionPlanDto[] = [];
  billingCycles: EnumOption[] = [];
  subscriptionStatuses: EnumOption[] = [];

  selectedPlan: number = 0;
  selectedCycle: number = 0;

  customAmount?: number;
  notes = '';

  readonly SubscriptionStatus = SubscriptionStatus;
  readonly BillingCycle = BillingCycle;

  constructor(
    private dialogRef: MatDialogRef<ManageSubscriptionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { school: SchoolWithSubscription },
    private schoolService: SchoolService,
    private enumService: EnumService,
    private planService: SubscriptionPlanService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.isLoading = true;

    forkJoin({
      plans: this.planService.getAllPlans(),
      cycles: this.enumService.getBillingCycles(),
      statuses: this.enumService.getSubscriptionStatuses()
    }).subscribe({
      next: ({ plans, cycles, statuses }) => {
        this.subscriptionPlans = plans;
        this.billingCycles = cycles;
        this.subscriptionStatuses = statuses;

        // Initialize selected plan and cycle
        if (this.data.school.subscription) {
          this.selectedPlan = Number(this.data.school.subscription.plan);
          this.selectedCycle = Number(this.data.school.subscription.billingCycle);
        } else {
          // Set defaults for new subscription
          this.selectedPlan = plans.length ? Number(plans[0].planValue) : 0;
          const monthlyCycle = cycles.find(c => c.name === 'Monthly');
          this.selectedCycle = monthlyCycle ? Number(monthlyCycle.value) : (cycles.length ? Number(cycles[0].value) : 0);
        }

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load subscription options:', err);
        this.showError('Failed to load subscription options');
        this.isLoading = false;
      }
    });
  }

  // ================== Plan Helpers ==================
  getSelectedPlan(): SubscriptionPlanDto | null {
    const plan = this.subscriptionPlans.find(p => p.planValue === this.selectedPlan);
    return plan || null;
  }

  getPlanForValue(planValue: number): SubscriptionPlanDto | undefined {
    return this.subscriptionPlans.find(p => p.planValue === planValue);
  }

  getPlanName(planValue: number): string {
    const plan = this.getPlanForValue(planValue);
    return plan?.name ?? 'Unknown Plan';
  }

  getPlanPriceForPlan(planValue: number): number {
    const plan = this.getPlanForValue(planValue);
    if (!plan) return 0;
    
    // Use custom amount if provided
    if (this.customAmount && this.customAmount > 0) {
      return Number(this.customAmount);
    }
    
    switch (Number(this.selectedCycle)) {
      case BillingCycle.Quarterly: 
        return Number(plan.quarterlyPrice || 0);
      case BillingCycle.Yearly: 
        return Number(plan.yearlyPrice || 0);
      case BillingCycle.Monthly:
      default: 
        return Number(plan.monthlyPrice || 0);
    }
  }

  getPlanPrice(): number {
    return this.getPlanPriceForPlan(this.selectedPlan);
  }

  getPlanAmount(): number {
    return this.getPlanPrice();
  }

  // ================== Cycle Helpers ==================
  getBillingCycleName(value: number): string {
    return this.enumService.getDisplayNameByValue(this.billingCycles, value);
  }

  getCycleSuffix(): string {
    switch (Number(this.selectedCycle)) {
      case BillingCycle.Quarterly: return 'per quarter';
      case BillingCycle.Yearly: return 'per year';
      case BillingCycle.Monthly:
      default: return 'per month';
    }
  }

  getCycleDescription(): string {
    switch (Number(this.selectedCycle)) {
      case BillingCycle.Quarterly: return 'quarterly';
      case BillingCycle.Yearly: return 'yearly';
      case BillingCycle.Monthly:
      default: return 'monthly';
    }
  }

  // ================== Status Helpers ==================
  getStatusClass(): string {
    if (!this.data.school.subscription) return 'bg-secondary';
    return this.enumService.getCssClassByValue(this.subscriptionStatuses, this.data.school.subscription.status);
  }

  getStatusDisplayName(): string {
    if (!this.data.school.subscription) return 'No Subscription';
    return this.enumService.getDisplayNameByValue(this.subscriptionStatuses, this.data.school.subscription.status);
  }

  canActivateSubscription(): boolean {
    if (!this.data.school.subscription) return false;
    const status = this.data.school.subscription.status;
    return status === SubscriptionStatus.Suspended || status === SubscriptionStatus.Expired;
  }



  // ================== Subscription Actions ==================
  async createNewSubscription(): Promise<void> {
    try {
      const plan = this.getSelectedPlan();
      if (!plan) {
        this.showError('Please select a plan');
        return;
      }

      const amount = this.getPlanAmount();
      const accountRef = `SUB-${this.data.school.id.substring(0, 8)}`;
      const description = `${plan.name} ${this.getCycleSuffix()} subscription`;

      let paymentReference = '';
      let paymentDate = new Date().toISOString();

      // Only open M-Pesa dialog if amount is greater than 0
      if (amount > 0) {
        const paymentResult = await this.openMpesaPayment(amount, accountRef, description);
        paymentReference = paymentResult.mpesaReceiptNumber || '';
        paymentDate = paymentResult.transactionDate || paymentDate;
      } else {
        // For zero amount, confirm with user
        if (!confirm('This subscription has no cost. Do you want to proceed?')) {
          return;
        }
        paymentReference = `FREE-${Date.now()}`;
      }

      this.isLoading = true;

      const payload = {
        schoolId: this.data.school.id,
        plan: this.selectedPlan,
        billingCycle: Number(this.selectedCycle),
        amount: Number(amount),
        currency: plan.currency || 'KES',
        maxStudents: Number(plan.maxStudents || 0),
        maxTeachers: Number(plan.maxTeachers || 0),
        maxStorageGB: Number(plan.maxStorageGB || 0),
        enabledFeatures: plan.enabledFeatures || [],
        notes: this.notes || '',
        paymentReference: paymentReference,
        paymentDate: paymentDate
      };

      this.schoolService.createSubscription(this.data.school.id, payload).subscribe({
        next: res => {
          this.isLoading = false;
          if (res.success) {
            this.showSuccess('Subscription created successfully');
            this.dialogRef.close({ refresh: true });
          } else {
            this.showError(res.message || 'Failed to create subscription');
          }
        },
        error: (err) => {
          console.error('Error creating subscription:', err);
          this.showError('Failed to create subscription');
          this.isLoading = false;
        }
      });
    } catch (error: any) {
      console.error('Create subscription error:', error);
      if (error.message !== 'Payment cancelled') {
        this.showError(error.message || 'Payment failed');
      }
      this.isLoading = false;
    }
  }

    private openMpesaPayment(amount: number, accountRef: string, description: string): Promise<any> {
    return new Promise((resolve, reject) => {
      const dialogRef = this.dialog.open(MpesaPaymentDialogComponent, {
        width: '500px',
        disableClose: true,
        data: {
          amount,
          accountReference: accountRef,
          description,
          phoneNumber: this.data.school.phoneNumber // Pre-fill if available
        }
      });

      dialogRef.afterClosed().subscribe(result => {
        if (result && result.success) {
          resolve(result);
        } else if (result && result.cancelled) {
          reject(new Error('Payment cancelled'));
        } else {
          reject(new Error('Payment failed'));
        }
      });
    });
  }

  async renewSubscription(): Promise<void> {
    if (!this.data.school.subscription) return;

    try {
      const plan = this.getSelectedPlan();
      if (!plan) {
        this.showError('Please select a plan');
        return;
      }

      const amount = this.getPlanAmount();
      const accountRef = `RNW-${this.data.school.subscription.id.substring(0, 8)}`;
      const description = `Renew ${plan.name} subscription`;

      let paymentReference = '';
      let paymentDate = new Date().toISOString();

      // Only open M-Pesa dialog if amount is greater than 0
      if (amount > 0) {
        const paymentResult = await this.openMpesaPayment(amount, accountRef, description);
        paymentReference = paymentResult.mpesaReceiptNumber || '';
        paymentDate = paymentResult.transactionDate || paymentDate;
      } else {
        // For zero amount, confirm with user
        if (!confirm('This renewal has no cost. Do you want to proceed?')) {
          return;
        }
        paymentReference = `FREE-RNW-${Date.now()}`;
      }

      this.isLoading = true;

      this.schoolService.renewSubscription(
        this.data.school.subscription.id, 
        Number(this.selectedCycle),
        paymentReference,
        paymentDate
      ).subscribe({
        next: res => {
          this.isLoading = false;
          if (res.success) {
            this.showSuccess('Subscription renewed successfully');
            this.dialogRef.close({ refresh: true });
          } else {
            this.showError(res.message || 'Failed to renew subscription');
          }
        },
        error: (err) => {
          console.error('Error renewing subscription:', err);
          this.showError('Failed to renew subscription');
          this.isLoading = false;
        }
      });
    } catch (error: any) {
      console.error('Renew subscription error:', error);
      if (error.message !== 'Payment cancelled') {
        this.showError(error.message || 'Payment failed');
      }
      this.isLoading = false;
    }
  }

  suspendSubscription(): void {
    if (!this.data.school.subscription) return;
    const reason = prompt('Enter reason for suspension');
    if (!reason) return;

    this.isLoading = true;
    this.schoolService.suspendSubscription(this.data.school.subscription.id, reason).subscribe({
      next: res => {
        this.isLoading = false;
        if (res.success) {
          this.showSuccess('Subscription suspended');
          this.dialogRef.close({ refresh: true });
        } else {
          this.showError(res.message || 'Failed to suspend');
        }
      },
      error: (err) => {
        console.error('Error suspending subscription:', err);
        this.showError('Failed to suspend subscription');
        this.isLoading = false;
      }
    });
  }

  activateSubscription(): void {
    if (!this.data.school.subscription) return;
    this.isLoading = true;
    this.schoolService.activateSubscription(this.data.school.subscription.id).subscribe({
      next: res => {
        this.isLoading = false;
        if (res.success) {
          this.showSuccess('Subscription activated');
          this.dialogRef.close({ refresh: true });
        } else {
          this.showError(res.message || 'Failed to activate');
        }
      },
      error: (err) => {
        console.error('Error activating subscription:', err);
        this.showError('Failed to activate subscription');
        this.isLoading = false;
      }
    });
  }

  cancelSubscription(): void {
    if (!this.data.school.subscription) return;
    if (!confirm('Cancel this subscription?')) return;

    this.isLoading = true;
    this.schoolService.cancelSubscription(this.data.school.subscription.id).subscribe({
      next: res => {
        this.isLoading = false;
        if (res.success) {
          this.showSuccess('Subscription cancelled');
          this.dialogRef.close({ refresh: true });
        } else {
          this.showError(res.message || 'Failed to cancel');
        }
      },
      error: (err) => {
        console.error('Error cancelling subscription:', err);
        this.showError('Failed to cancel subscription');
        this.isLoading = false;
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  // ================== UI Helpers ==================
  formatKES(amount?: number): string {
    const value = Number(amount || 0);
    return `KSh ${value.toLocaleString('en-KE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }

  private showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['alert', 'alert-success'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['alert', 'alert-danger'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }
}