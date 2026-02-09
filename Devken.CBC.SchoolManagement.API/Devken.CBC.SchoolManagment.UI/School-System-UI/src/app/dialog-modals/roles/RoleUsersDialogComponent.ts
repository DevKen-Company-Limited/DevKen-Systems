import { Component, Inject, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';

import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { UserWithPermission } from 'app/core/DevKenService/Types/role-permissions';

export interface RoleUsersDialogData {
    role: any;
    users: UserWithPermission[];
    title?: string;
}

@Component({
    selector: 'app-role-users-dialog',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatDividerModule,
        MatChipsModule,
        MatFormFieldModule,
        MatInputModule,
        MatPaginatorModule
    ],
    template: `
        <div class="flex flex-col max-h-[85vh]">
            <!-- Header -->
            <div class="flex items-start gap-4 p-6 pb-4">
                <div class="w-12 h-12 rounded-lg bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center shadow-sm flex-shrink-0">
                    <mat-icon class="text-white icon-size-6" [svgIcon]="'heroicons_outline:users'"></mat-icon>
                </div>
                <div class="flex-1 min-w-0">
                    <h2 mat-dialog-title class="text-2xl font-bold text-gray-900 dark:text-white mb-1">
                        {{ data.title || 'Users in Role' }}
                    </h2>
                    <div class="text-sm text-gray-600 dark:text-gray-400">
                        <strong>{{ data.role?.name || data.role?.roleName }}</strong>
                    </div>
                </div>
                <button mat-icon-button [mat-dialog-close] class="flex-shrink-0" matTooltip="Close">
                    <mat-icon [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
                </button>
            </div>

            <mat-divider></mat-divider>

            <!-- Statistics & Search -->
            <div class="p-6 pb-4 bg-gray-50 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700">
                <!-- Statistics -->
                <div class="mb-4 p-4 bg-blue-50 dark:bg-blue-900 rounded-lg border border-blue-200 dark:border-blue-700">
                    <div class="flex items-center justify-between">
                        <div class="flex items-center gap-2 text-blue-900 dark:text-blue-100">
                            <mat-icon class="icon-size-5" [svgIcon]="'heroicons_outline:information-circle'"></mat-icon>
                            <span class="font-semibold">
                                {{ filteredUsers.length }} user{{ filteredUsers.length !== 1 ? 's' : '' }} assigned to this role
                            </span>
                        </div>
                        <div *ngIf="searchTerm" class="text-xs text-blue-700 dark:text-blue-300">
                            Filtered results
                        </div>
                    </div>
                </div>

                <!-- Search -->
                <mat-form-field class="w-full" appearance="outline">
                    <mat-label>Search users</mat-label>
                    <input
                        matInput
                        [(ngModel)]="searchTerm"
                        (ngModelChange)="onSearchChange($event)"
                        placeholder="Name or email"
                        autocomplete="off">
                    <mat-icon matPrefix [svgIcon]="'heroicons_outline:magnifying-glass'"></mat-icon>
                    <button *ngIf="searchTerm" mat-icon-button matSuffix (click)="clearSearch()" matTooltip="Clear search">
                        <mat-icon [svgIcon]="'heroicons_outline:x-mark'"></mat-icon>
                    </button>
                </mat-form-field>
            </div>

            <!-- Content -->
            <div mat-dialog-content class="flex-1 overflow-y-auto p-6">
                <!-- Users List -->
                <div class="space-y-3" *ngIf="paginatedUsers.length > 0">
                    <div 
                        *ngFor="let user of paginatedUsers; trackBy: trackByUserId"
                        class="p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:shadow-md transition-shadow bg-white dark:bg-gray-800">
                        <div class="flex items-start gap-3">
                            <!-- Avatar -->
                            <div class="w-10 h-10 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center shadow-sm text-white font-semibold text-sm flex-shrink-0">
                                {{ getUserInitials(user.fullName) }}
                            </div>
                            
                            <!-- User Info -->
                            <div class="flex-1 min-w-0">
                                <div class="font-semibold text-gray-900 dark:text-white mb-1">
                                    {{ user.fullName }}
                                </div>
                                <div class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-2">
                                    <mat-icon class="icon-size-4" [svgIcon]="'heroicons_outline:envelope'"></mat-icon>
                                    <span class="truncate">{{ user.email }}</span>
                                </div>
                                
                                <!-- Additional Roles -->
                                <div class="flex flex-wrap gap-1.5 items-center" *ngIf="user.roleNames && user.roleNames.length > 1">
                                    <span class="text-xs text-gray-500 dark:text-gray-400 flex-shrink-0">Other Roles:</span>
                                    <mat-chip-row 
                                        *ngFor="let roleName of getOtherRoles(user.roleNames); trackBy: trackByRoleName"
                                        class="text-xs h-6">
                                        <mat-icon class="icon-size-3 mr-1" [svgIcon]="'heroicons_outline:shield-check'"></mat-icon>
                                        {{ roleName }}
                                    </mat-chip-row>
                                </div>
                                <div class="text-xs text-gray-400 italic" *ngIf="!user.roleNames || user.roleNames.length <= 1">
                                    No other roles
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Empty State - No Results -->
                <div *ngIf="filteredUsers.length === 0 && searchTerm" class="text-center py-12">
                    <mat-icon class="icon-size-16 mb-4 text-gray-400 mx-auto" [svgIcon]="'heroicons_outline:magnifying-glass'"></mat-icon>
                    <h3 class="text-lg font-semibold mb-2 text-gray-900 dark:text-white">No Results Found</h3>
                    <p class="text-gray-500 dark:text-gray-400 mb-4">
                        No users match "{{ searchTerm }}"
                    </p>
                    <button mat-stroked-button color="primary" (click)="clearSearch()">
                        Clear Search
                    </button>
                </div>

                <!-- Empty State - No Users -->
                <div *ngIf="data.users.length === 0" class="text-center py-12">
                    <mat-icon class="icon-size-16 mb-4 text-gray-400 mx-auto" [svgIcon]="'heroicons_outline:user-group'"></mat-icon>
                    <h3 class="text-lg font-semibold mb-2 text-gray-900 dark:text-white">No Users Found</h3>
                    <p class="text-gray-500 dark:text-gray-400">
                        No users currently assigned to this role
                    </p>
                </div>
            </div>

            <!-- Pagination -->
            <div *ngIf="filteredUsers.length > pageSize" class="border-t border-gray-200 dark:border-gray-700">
                <mat-paginator 
                    #paginator
                    [length]="filteredUsers.length" 
                    [pageSize]="pageSize" 
                    [pageSizeOptions]="[5, 10, 20, 50]"
                    [pageIndex]="currentPage"
                    (page)="onPageChange($event)"
                    showFirstLastButtons>
                </mat-paginator>
            </div>

            <mat-divider></mat-divider>

            <!-- Footer -->
            <div mat-dialog-actions class="flex justify-between items-center gap-2 p-4">
                <div class="text-sm text-gray-500 dark:text-gray-400">
                    Showing {{ getShowingRange() }} of {{ filteredUsers.length }} user{{ filteredUsers.length !== 1 ? 's' : '' }}
                </div>
                <button mat-flat-button color="primary" [mat-dialog-close]>
                    Close
                </button>
            </div>
        </div>
    `,
    styles: [`
        :host ::ng-deep .mat-mdc-dialog-container {
            --mdc-dialog-container-shape: 12px;
        }

        :host ::ng-deep .mat-mdc-paginator {
            background-color: transparent;
        }
    `]
})
export class RoleUsersDialogComponent implements OnInit, OnDestroy {
    @ViewChild(MatPaginator) paginator!: MatPaginator;

    private readonly unsubscribe$ = new Subject<void>();
    private readonly searchSubject$ = new Subject<string>();

    // All users from dialog data
    allUsers: UserWithPermission[] = [];
    
    // Filtered users based on search
    filteredUsers: UserWithPermission[] = [];
    
    // Paginated users for current page
    paginatedUsers: UserWithPermission[] = [];

    // Search and pagination state
    searchTerm = '';
    currentPage = 0;
    pageSize = 10;

    constructor(
        @Inject(MAT_DIALOG_DATA) public data: RoleUsersDialogData,
        private dialogRef: MatDialogRef<RoleUsersDialogComponent>
    ) {}

    ngOnInit(): void {
        // Initialize users
        this.allUsers = this.data.users || [];
        this.filteredUsers = [...this.allUsers];
        this.updatePaginatedUsers();

        // Setup search with debounce
        this.setupSearchDebounce();
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    /**
     * Setup search debounce
     */
    private setupSearchDebounce(): void {
        this.searchSubject$.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            takeUntil(this.unsubscribe$)
        ).subscribe(() => {
            this.applyFilter();
        });
    }

    /**
     * Handle search input change
     */
    onSearchChange(value: string): void {
        this.searchSubject$.next(value);
    }

    /**
     * Apply search filter
     */
    private applyFilter(): void {
        if (!this.searchTerm || !this.searchTerm.trim()) {
            this.filteredUsers = [...this.allUsers];
        } else {
            const term = this.searchTerm.toLowerCase().trim();
            this.filteredUsers = this.allUsers.filter(user =>
                user.fullName?.toLowerCase().includes(term) ||
                user.email?.toLowerCase().includes(term)
            );
        }

        // Reset to first page when filter changes
        this.currentPage = 0;
        this.updatePaginatedUsers();
    }

    /**
     * Clear search
     */
    clearSearch(): void {
        this.searchTerm = '';
        this.applyFilter();
    }

    /**
     * Handle page change
     */
    onPageChange(event: PageEvent): void {
        this.currentPage = event.pageIndex;
        this.pageSize = event.pageSize;
        this.updatePaginatedUsers();
    }

    /**
     * Update paginated users based on current page
     */
    private updatePaginatedUsers(): void {
        const startIndex = this.currentPage * this.pageSize;
        const endIndex = startIndex + this.pageSize;
        this.paginatedUsers = this.filteredUsers.slice(startIndex, endIndex);
    }

    /**
     * Get showing range text
     */
    getShowingRange(): string {
        if (this.filteredUsers.length === 0) {
            return '0';
        }

        const start = (this.currentPage * this.pageSize) + 1;
        const end = Math.min((this.currentPage + 1) * this.pageSize, this.filteredUsers.length);
        
        return `${start}-${end}`;
    }

    /**
     * Get user initials for avatar
     */
    getUserInitials(fullName: string): string {
        if (!fullName?.trim()) return '?';
        const names = fullName.trim().split(' ');
        if (names.length >= 2) {
            return (names[0][0] + names[names.length - 1][0]).toUpperCase();
        }
        return fullName.substring(0, 2).toUpperCase();
    }

    /**
     * Get other roles (excluding current role)
     */
    getOtherRoles(roleNames: string[]): string[] {
        if (!roleNames || !Array.isArray(roleNames)) {
            return [];
        }
        
        const currentRoleName = this.data.role?.name || this.data.role?.roleName;
        return roleNames.filter(name => name !== currentRoleName);
    }

    /**
     * TrackBy function for users
     */
    trackByUserId(index: number, user: UserWithPermission): string {
        return user.userId || index.toString();
    }

    /**
     * TrackBy function for role names
     */
    trackByRoleName(index: number, roleName: string): string {
        return roleName || index.toString();
    }
}