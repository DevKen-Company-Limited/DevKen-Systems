import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
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

  selectedPlan!: number;
  selectedCycle!: number;

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
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadData();

    // Initialize selected plan/cycle if subscription exists
    if (this.data.school.subscription) {
      this.selectedPlan = Number(this.data.school.subscription.plan);
      this.selectedCycle = Number(this.data.school.subscription.billingCycle);
    }
  }

  private loadData(): void {
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

        // Ensure selectedPlan is never null
        if (!this.selectedPlan && plans.length > 0) {
          this.selectedPlan = Number(plans[0].planValue);
        }

        if (!this.selectedCycle && cycles.length > 0) {
          this.selectedCycle = Number(cycles.find(c => c.name === 'Monthly')?.value ?? cycles[0].value);
        }

        this.isLoading = false;
      },
      error: () => {
        this.showError('Failed to load subscription options');
        this.isLoading = false;
      }
    });
  }

  // ================== Plan Helpers ==================
  getSelectedPlan(): SubscriptionPlanDto {
    const plan = this.subscriptionPlans.find(p => p.planValue === this.selectedPlan);
    if (!plan) throw new Error(`Selected plan ${this.selectedPlan} not found`);
    return plan;
  }

  getPlanForValue(planValue: number): SubscriptionPlanDto | undefined {
    return this.subscriptionPlans.find(p => p.planValue === planValue);
  }

  getPlanName(planValue: number): string {
    const plan = this.getPlanForValue(planValue);
    return plan?.name ?? 'Unknown Plan';
  }

  // Get price for a specific plan and current billing cycle selection
  getPlanPriceForPlan(planValue: number): number {
    const plan = this.getPlanForValue(planValue);
    if (!plan) return 0;

    if (this.customAmount) return Number(this.customAmount);

    switch (Number(this.selectedCycle)) {
      case BillingCycle.Quarterly: return Number(plan.quarterlyPrice);
      case BillingCycle.Yearly: return Number(plan.yearlyPrice);
      case BillingCycle.Monthly:
      default: return Number(plan.monthlyPrice);
    }
  }

  // Get price for the currently selected plan
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
  createNewSubscription(): void {
    try {
      const plan = this.getSelectedPlan();
      this.isLoading = true;

      const payload = {
        schoolId: this.data.school.id,
        plan: this.selectedPlan,
        billingCycle: Number(this.selectedCycle),
        amount: Number(this.getPlanAmount()),
        currency: plan.currency || 'KES',
        maxStudents: Number(plan.maxStudents || 0),
        maxTeachers: Number(plan.maxTeachers || 0),
        maxStorageGB: Number(plan.maxStorageGB || 0),
        enabledFeatures: plan.enabledFeatures || [],
        notes: this.notes || ''
      };

      console.log('Creating subscription payload:', payload);

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
        error: (error) => {
          console.error('Create subscription error:', error);
          this.showError('Failed to create subscription');
          this.isLoading = false;
        }
      });
    } catch (err: any) {
      this.showError(err.message);
      this.isLoading = false;
    }
  }

  renewSubscription(): void {
    if (!this.data.school.subscription) return;
    this.isLoading = true;

    // The service expects only billingCycle as parameter
    this.schoolService.renewSubscription(this.data.school.subscription.id, Number(this.selectedCycle))
      .subscribe({
        next: res => {
          this.isLoading = false;
          if (res.success) {
            this.showSuccess('Subscription renewed successfully');
            this.dialogRef.close({ refresh: true });
          } else {
            this.showError(res.message || 'Failed to renew subscription');
          }
        },
        error: (error) => {
          console.error('Renew subscription error:', error);
          this.showError('Failed to renew subscription');
          this.isLoading = false;
        }
      });
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
      error: (error) => {
        console.error('Suspend subscription error:', error);
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
      error: (error) => {
        console.error('Activate subscription error:', error);
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
      error: (error) => {
        console.error('Cancel subscription error:', error);
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
    return `KSh ${Number(amount || 0).toLocaleString('en-KE')}`;
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