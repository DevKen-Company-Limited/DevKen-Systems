import { CommonModule } from '@angular/common';
import { Component, ViewEncapsulation, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';

interface Student {
  admissionNo: string;
  name: string;
  initials: string;
  gender: string;
  class: string;
  performance: number;
  status: 'Active' | 'Inactive' | 'Pending';
  avatarColor: string;
  expanded?: boolean; // For toggled actions
}

@Component({
  selector: 'app-page-design-version-one',
  templateUrl: './page-design-version-one.component.html',
  styleUrl: './page-design-version-one.component.scss',
  encapsulation: ViewEncapsulation.None,
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    RouterLink,
    MatButtonModule,
    FormsModule,
    DragDropModule
  ],
})
export class PageDesignVersionOneComponent implements OnInit {
  // Sample data for demonstration
  totalStudents: number = 450;
  averageScore: number = 82;
  pendingTasks: number = 12;

  // Filter properties
  searchQuery: string = '';
  selectedClass: string = 'all';
  selectedStatus: string = 'all';
  selectedGender: string = 'all';
  selectedTerm: string = 'all';
  selectedSubject: string = 'all';
  selectedAttendance: string = 'all';
  selectedFeeStatus: string = 'all';
  selectedHouse: string = 'all';
  selectedTransport: string = 'all';
  minPerformance: number = 0;
  maxPerformance: number = 100;
  minAge: number = 5;
  maxAge: number = 18;
  showFilterPanel: boolean = false;
  showAdvancedFilters: boolean = false;
  showColumnsPanel: boolean = false; // toggles the columns visibility panel

  // Available options for filtering
  availableClasses: string[] = ['all', 'Grade 4A', 'Grade 5A', 'Grade 5B', 'Grade 6A'];
  availableStatuses: string[] = ['all', 'Active', 'Inactive', 'Pending'];
  availableGenders: string[] = ['all', 'Male', 'Female'];
  availableTerms: string[] = ['all', 'Term 1', 'Term 2', 'Term 3'];
  availableSubjects: string[] = ['all', 'Mathematics', 'English', 'Science', 'Kiswahili', 'Social Studies', 'CRE'];
  availableAttendance: string[] = ['all', 'Excellent (>95%)', 'Good (85-95%)', 'Fair (75-85%)', 'Poor (<75%)'];
  availableFeeStatus: string[] = ['all', 'Paid', 'Partial', 'Pending', 'Overdue'];
  availableHouses: string[] = ['all', 'Red House', 'Blue House', 'Green House', 'Yellow House'];
  availableTransport: string[] = ['all', 'School Bus', 'Private', 'Walking'];

  // Column definitions (controls order, label, and visibility)
  columns: Array<{ key: string; label: string; visible: boolean }> = [
    { key: 'admissionNo', label: 'Admission No.', visible: true },
    { key: 'name', label: 'Student Name', visible: true },
    { key: 'class', label: 'Class', visible: true },
    { key: 'performance', label: 'Performance', visible: true },
    { key: 'status', label: 'Status', visible: true },
    { key: 'actions', label: 'Actions', visible: true }
  ];

  // Snapshot of original columns (for reset)
  private originalColumns: Array<{ key: string; label: string; visible: boolean }> = [];

  // All students data
  allStudents: Student[] = [
    {
      admissionNo: 'STU2024001',
      name: 'John Mwangi',
      initials: 'JM',
      gender: 'Male',
      class: 'Grade 5A',
      performance: 92,
      status: 'Active',
      avatarColor: 'from-blue-500 to-indigo-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024002',
      name: 'Grace Akinyi',
      initials: 'GA',
      gender: 'Female',
      class: 'Grade 5A',
      performance: 78,
      status: 'Active',
      avatarColor: 'from-pink-500 to-rose-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024003',
      name: 'David Kamau',
      initials: 'DK',
      gender: 'Male',
      class: 'Grade 5B',
      performance: 85,
      status: 'Active',
      avatarColor: 'from-emerald-500 to-teal-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024004',
      name: 'Faith Njeri',
      initials: 'FN',
      gender: 'Female',
      class: 'Grade 4A',
      performance: 91,
      status: 'Active',
      avatarColor: 'from-purple-500 to-violet-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024005',
      name: 'Brian Omondi',
      initials: 'BO',
      gender: 'Male',
      class: 'Grade 6A',
      performance: 73,
      status: 'Pending',
      avatarColor: 'from-amber-500 to-orange-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024006',
      name: 'Mary Wanjiru',
      initials: 'MW',
      gender: 'Female',
      class: 'Grade 5A',
      performance: 88,
      status: 'Active',
      avatarColor: 'from-cyan-500 to-blue-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024007',
      name: 'Peter Kibet',
      initials: 'PK',
      gender: 'Male',
      class: 'Grade 5B',
      performance: 67,
      status: 'Inactive',
      avatarColor: 'from-red-500 to-pink-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024008',
      name: 'Sarah Adhiambo',
      initials: 'SA',
      gender: 'Female',
      class: 'Grade 4A',
      performance: 94,
      status: 'Active',
      avatarColor: 'from-indigo-500 to-purple-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024009',
      name: 'Michael Otieno',
      initials: 'MO',
      gender: 'Male',
      class: 'Grade 5A',
      performance: 56,
      status: 'Inactive',
      avatarColor: 'from-teal-500 to-cyan-600',
      expanded: false
    },
    {
      admissionNo: 'STU2024010',
      name: 'Emily Wambui',
      initials: 'EW',
      gender: 'Female',
      class: 'Grade 6A',
      performance: 99,
      status: 'Active',
      avatarColor: 'from-lime-500 to-green-600',
      expanded: false
    }
  ];

  // Filtered students (computed property)
  get filteredStudents(): Student[] {
    return this.allStudents.filter(student => {
      const matchesSearch = this.searchQuery === '' ||
        student.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        student.admissionNo.toLowerCase().includes(this.searchQuery.toLowerCase());

      const matchesClass = this.selectedClass === 'all' || student.class === this.selectedClass;

      const matchesStatus = this.selectedStatus === 'all' || student.status === this.selectedStatus;

      const matchesGender = this.selectedGender === 'all' || student.gender === this.selectedGender;

      const matchesTerm = this.selectedTerm === 'all'; // Simplified for demo

      const matchesSubject = this.selectedSubject === 'all'; // Simplified for demo

      const matchesAttendance = this.selectedAttendance === 'all'; // Simplified for demo

      const matchesFeeStatus = this.selectedFeeStatus === 'all'; // Simplified for demo

      const matchesHouse = this.selectedHouse === 'all'; // Simplified for demo

      const matchesTransport = this.selectedTransport === 'all'; // Simplified for demo

      const matchesPerformance = student.performance >= this.minPerformance &&
        student.performance <= this.maxPerformance;

      const matchesAge = this.minAge <= this.maxAge; // Simplified for demo (actual age not in data)

      return matchesSearch && matchesClass && matchesStatus && matchesGender &&
        matchesTerm && matchesSubject && matchesAttendance && matchesFeeStatus &&
        matchesHouse && matchesTransport && matchesPerformance && matchesAge;
    });
  }

  // Pagination
  currentPage: number = 1;
  itemsPerPage: number = 5;

  get paginatedStudents(): Student[] {
    // ensure currentPage is within range
    if (this.currentPage > this.totalPages) {
      this.currentPage = Math.max(1, this.totalPages);
    }
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    const endIndex = startIndex + this.itemsPerPage;
    return this.filteredStudents.slice(startIndex, endIndex);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredStudents.length / this.itemsPerPage);
  }

  get showingFrom(): number {
    return this.filteredStudents.length === 0 ? 0 : (this.currentPage - 1) * this.itemsPerPage + 1;
  }

  get showingTo(): number {
    const to = this.currentPage * this.itemsPerPage;
    return to > this.filteredStudents.length ? this.filteredStudents.length : to;
  }

  // Visible columns helper
  get visibleColumns() {
    return this.columns.filter(c => c.visible);
  }

  // Check if any filters are active
  get hasActiveFilters(): boolean {
    return this.searchQuery !== '' ||
      this.selectedClass !== 'all' ||
      this.selectedStatus !== 'all' ||
      this.selectedGender !== 'all' ||
      this.selectedTerm !== 'all' ||
      this.selectedSubject !== 'all' ||
      this.selectedAttendance !== 'all' ||
      this.selectedFeeStatus !== 'all' ||
      this.selectedHouse !== 'all' ||
      this.selectedTransport !== 'all' ||
      this.minPerformance !== 0 ||
      this.maxPerformance !== 100 ||
      this.minAge !== 5 ||
      this.maxAge !== 18;
  }

  constructor() { }

  ngOnInit(): void {
    // snapshot original layout for reset
    this.originalColumns = JSON.parse(JSON.stringify(this.columns));
  }

  /**
   * Toggle filter panel visibility
   */
  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.searchQuery = '';
    this.selectedClass = 'all';
    this.selectedStatus = 'all';
    this.selectedGender = 'all';
    this.selectedTerm = 'all';
    this.selectedSubject = 'all';
    this.selectedAttendance = 'all';
    this.selectedFeeStatus = 'all';
    this.selectedHouse = 'all';
    this.selectedTransport = 'all';
    this.minPerformance = 0;
    this.maxPerformance = 100;
    this.minAge = 5;
    this.maxAge = 18;
    this.currentPage = 1;
  }

  /**
   * Toggle student actions row
   * Closes other rows' open action panels and toggles the target row.
   */
  toggleStudentActions(student: Student): void {
    // Close all other expanded rows
    this.allStudents.forEach(s => {
      if (s !== student) {
        s.expanded = false;
      }
    });
    // Toggle current row
    student.expanded = !student.expanded;
  }

  /**
   * Helper to close all action panels (useful if you add outside click behavior)
   */
  closeAllStudentActions(): void {
    this.allStudents.forEach(s => s.expanded = false);
  }

  /**
   * Handle student actions
   * Each method closes the action panel after performing the action.
   */
  viewStudent(student: Student): void {
    console.log('Viewing student:', student);
    student.expanded = false;
  }

  editStudent(student: Student): void {
    console.log('Editing student:', student);
    student.expanded = false;
  }

  deleteStudent(student: Student): void {
    console.log('Deleting student:', student);
    student.expanded = false;
  }

  printReport(student: Student): void {
    console.log('Printing report for:', student);
    student.expanded = false;
  }

  sendMessage(student: Student): void {
    console.log('Sending message to:', student);
    student.expanded = false;
  }

  /**
   * Go to previous page
   */
  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  /**
   * Go to next page
   */
  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  /**
   * Get performance badge color
   */
  getPerformanceBadgeColor(performance: number): string {
    if (performance >= 90) return 'text-green-600';
    if (performance >= 80) return 'text-blue-600';
    if (performance >= 70) return 'text-amber-600';
    return 'text-red-600';
  }

  /**
   * Get status badge classes
   */
  getStatusBadgeClasses(status: string): string {
    switch (status) {
      case 'Active':
        return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400';
      case 'Inactive':
        return 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300';
      case 'Pending':
        return 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400';
      default:
        return 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300';
    }
  }

  /**
   * Handle column drop (reorder).
   * Because some columns can be hidden we map the visible indices back to the full columns array indices,
   * then perform a move on the underlying columns array so the order is preserved for visible/hidden columns.
   */
  dropColumn(event: CdkDragDrop<any[]>): void {
    // build map of visible column indices into full columns array
    const visibleIndexes = this.columns
      .map((col, idx) => (col.visible ? idx : -1))
      .filter(idx => idx >= 0);

    const prevVisibleIndex = event.previousIndex;
    const currVisibleIndex = event.currentIndex;

    // map to full array indices
    const fromIdx = visibleIndexes[prevVisibleIndex];
    const toIdx = visibleIndexes[currVisibleIndex];

    if (fromIdx === undefined || toIdx === undefined) {
      return;
    }

    moveItemInArray(this.columns, fromIdx, toIdx);
  }

  /**
   * Reset columns order to original snapshot
   */
  resetColumnsOrder(): void {
    // restore only order — preserve current visibility flags if possible
    const visibilityMap = new Map<string, boolean>();
    this.columns.forEach(c => visibilityMap.set(c.key, c.visible));

    // deep-copy original and restore visibility from current map if present
    this.columns = this.originalColumns.map(orig => ({
      key: orig.key,
      label: orig.label,
      visible: visibilityMap.has(orig.key) ? !!visibilityMap.get(orig.key) : orig.visible
    }));
  }

  /**
   * Reset columns visibility to original snapshot (preserve current order)
   */
  resetColumnsVisibility(): void {
    const orderKeys = this.columns.map(c => c.key); // keep order
    const originalVisibility = new Map<string, boolean>();
    this.originalColumns.forEach(c => originalVisibility.set(c.key, c.visible));

    // update current columns in-place to preserve order but restore visibility
    this.columns = orderKeys.map(key => {
      const orig = this.originalColumns.find(c => c.key === key);
      return {
        key,
        label: orig ? orig.label : key,
        visible: originalVisibility.has(key) ? !!originalVisibility.get(key) : true
      };
    });
  }

  /**
   * Full reset: restore both order and visibility to original snapshot
   */
  resetColumnsAll(): void {
    this.columns = JSON.parse(JSON.stringify(this.originalColumns));
  }
}