import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UserWithRoles } from 'app/core/DevKenService/Types/roles';

export interface UserDetailsDialogData {
    user: UserWithRoles;
}

@Component({
    selector: 'app-user-details-dialog',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatDividerModule,
        MatChipsModule,
        MatTooltipModule
    ],
    template: `
        <div class="flex flex-col">

            <!-- Header -->
            <div class="flex items-center justify-between px-6 py-4 border-b">
                <h2 class="text-2xl font-semibold">User Details</h2>
                <button mat-icon-button [mat-dialog-close]>
                    <mat-icon [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
                </button>
            </div>

            <!-- User Profile -->
            <div class="px-6 py-6">
                <div class="flex items-start gap-4 mb-6">
                    <div
                        class="w-20 h-20 rounded-full bg-gradient-to-br from-primary-400 to-primary-600
                               flex items-center justify-center shadow-lg">
                        <span class="text-white font-bold text-2xl">
                            {{ getUserInitials(data.user.fullName) }}
                        </span>
                    </div>

                    <div class="flex-1">
                        <h3 class="text-2xl font-bold mb-1">
                            {{ data.user.fullName }}
                        </h3>

                        <p class="text-secondary flex items-center gap-2 mb-2">
                            <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:envelope'"></mat-icon>
                            {{ data.user.email }}
                        </p>

                        <p class="text-secondary flex items-center gap-2">
                            <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:user-circle'"></mat-icon>
                            {{ data.user.firstName }}
                        </p>
                    </div>
                </div>

                <mat-divider class="my-4"></mat-divider>

                <!-- Roles Section -->
                <div class="mb-6">
                    <div class="flex items-center justify-between mb-3">
                        <h4 class="text-lg font-semibold flex items-center gap-2">
                            <mat-icon [svgIcon]="'heroicons_outline:shield-check'"></mat-icon>
                            Assigned Roles
                        </h4>

                        <span
                            class="px-3 py-1 text-sm bg-primary-100 dark:bg-primary-900
                                   text-primary-700 dark:text-primary-300
                                   rounded-full font-medium">
                            {{ data.user.roles.length }}
                        </span>
                    </div>

                    <ng-container *ngIf="data.user.roles.length > 0; else noRoles">
                        <div class="grid grid-cols-1 gap-3">
                            <div
                                *ngFor="let role of data.user.roles"
                                class="p-3 rounded-lg bg-gray-50 dark:bg-gray-800
                                       border border-gray-200 dark:border-gray-700">

                                <div class="flex items-center gap-3">
                                    <div
                                        class="w-10 h-10 rounded-full bg-primary-100 dark:bg-primary-900
                                               flex items-center justify-center">
                                        <mat-icon
                                            class="text-primary icon-size-5"
                                            [svgIcon]="'heroicons_outline:shield-check'">
                                        </mat-icon>
                                    </div>

                                    <div>
                                        <p class="font-semibold">{{ role.roleName }}</p>
                                        <p
                                            *ngIf="role.description"
                                            class="text-sm text-secondary">
                                            {{ role.description }}
                                        </p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </ng-container>

                    <ng-template #noRoles>
                        <div
                            class="text-center py-8 bg-gray-50 dark:bg-gray-800
                                   rounded-lg border border-dashed
                                   border-gray-300 dark:border-gray-600">
                            <mat-icon
                                class="icon-size-12 mb-2 text-gray-400"
                                [svgIcon]="'heroicons_outline:shield-exclamation'">
                            </mat-icon>
                            <p class="text-secondary">No roles assigned</p>
                        </div>
                    </ng-template>
                </div>

                <mat-divider class="my-4"></mat-divider>

                <!-- Permissions Section -->
                <div>
                    <div class="flex items-center justify-between mb-3">
                        <h4 class="text-lg font-semibold flex items-center gap-2">
                            <mat-icon [svgIcon]="'heroicons_outline:key'"></mat-icon>
                            Effective Permissions
                        </h4>

                        <span
                            class="px-3 py-1 text-sm bg-blue-100 dark:bg-blue-900
                                   text-blue-700 dark:text-blue-300
                                   rounded-full font-medium">
                            {{ data.user.permissions.length }}
                        </span>
                    </div>

                    <ng-container *ngIf="data.user.permissions.length > 0; else noPermissions">
                        <div class="flex flex-wrap gap-2">
                            <mat-chip
                                *ngFor="let permission of data.user.permissions"
                                class="bg-blue-50 dark:bg-blue-900/20
                                       text-blue-700 dark:text-blue-300">
                                <mat-icon
                                    class="icon-size-4 mr-1"
                                    [svgIcon]="'heroicons_outline:key'">
                                </mat-icon>
                                {{ permission }}
                            </mat-chip>
                        </div>
                    </ng-container>

                    <ng-template #noPermissions>
                        <div
                            class="text-center py-8 bg-gray-50 dark:bg-gray-800
                                   rounded-lg border border-dashed
                                   border-gray-300 dark:border-gray-600">
                            <mat-icon
                                class="icon-size-12 mb-2 text-gray-400"
                                [svgIcon]="'heroicons_outline:lock-closed'">
                            </mat-icon>
                            <p class="text-secondary">No permissions granted</p>
                        </div>
                    </ng-template>
                </div>
            </div>

            <!-- Footer -->
            <div class="flex justify-end px-6 py-4 border-t bg-gray-50 dark:bg-gray-800">
                <button mat-flat-button color="primary" [mat-dialog-close]>
                    Close
                </button>
            </div>

        </div>
    `,
    styles: [`
        :host {
            display: block;
        }

        .icon-size-4 {
            width: 1rem;
            height: 1rem;
        }

        .icon-size-5 {
            width: 1.25rem;
            height: 1.25rem;
        }

        .icon-size-12 {
            width: 3rem;
            height: 3rem;
        }
    `]
})
export class UserDetailsDialogComponent {
    constructor(
        private _dialogRef: MatDialogRef<UserDetailsDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: UserDetailsDialogData
    ) {}

    getUserInitials(fullName: string): string {
        if (!fullName) return '??';
        const names = fullName.trim().split(' ');
        if (names.length >= 2) {
            return (names[0][0] + names[names.length - 1][0]).toUpperCase();
        }
        return fullName.substring(0, 2).toUpperCase();
    }
}