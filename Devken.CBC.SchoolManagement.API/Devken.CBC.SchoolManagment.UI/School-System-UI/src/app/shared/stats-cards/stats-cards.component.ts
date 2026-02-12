import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

export interface StatCard {
  label: string;
  value: number | string;
  icon: string;
  iconColor: 'indigo' | 'green' | 'violet' | 'amber' | 'red' | 'blue' | 'orange' | 'pink';
  trend?: {
    value: number;
    isPositive: boolean;
  };
}

/**
 * Reusable Stats Cards Component
 * 
 * Displays a grid of statistic cards with icons, values, and optional trends.
 * 
 * @example
 * ```html
 * <app-stats-cards
 *   [cards]="statsCards"
 *   [columns]="4">
 * </app-stats-cards>
 * ```
 */
@Component({
  selector: 'app-stats-cards',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
  ],
  templateUrl: './stats-cards.component.html',
  styleUrls: ['./stats-cards.component.scss']
})
export class StatsCardsComponent {
  /** Array of stat cards to display */
  @Input() cards: StatCard[] = [];
  
  /** Number of columns in grid (2, 3, or 4) */
  @Input() columns: number = 4;
  
  /** Compact mode for smaller cards */
  @Input() compact: boolean = false;

  /**
   * Get grid column classes based on columns input
   */
  get gridColsClass(): string {
    const colsMap: { [key: number]: string } = {
      2: 'grid-cols-1 sm:grid-cols-2',
      3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
      4: 'grid-cols-2 md:grid-cols-4',
    };
    return colsMap[this.columns] || colsMap[4];
  }

  /**
   * Get icon background color classes
   */
  getIconBgClass(color: string): string {
    const colorMap: { [key: string]: string } = {
      indigo: 'bg-indigo-100 dark:bg-indigo-900/30',
      green: 'bg-green-100 dark:bg-green-900/30',
      violet: 'bg-violet-100 dark:bg-violet-900/30',
      amber: 'bg-amber-100 dark:bg-amber-900/30',
      red: 'bg-red-100 dark:bg-red-900/30',
      blue: 'bg-blue-100 dark:bg-blue-900/30',
      orange: 'bg-orange-100 dark:bg-orange-900/30',
      pink: 'bg-pink-100 dark:bg-pink-900/30',
    };
    return colorMap[color] || colorMap.indigo;
  }

  /**
   * Get icon text color classes
   */
  getIconTextClass(color: string): string {
    const colorMap: { [key: string]: string } = {
      indigo: 'text-indigo-600 dark:text-indigo-400',
      green: 'text-green-600 dark:text-green-400',
      violet: 'text-violet-600 dark:text-violet-400',
      amber: 'text-amber-600 dark:text-amber-400',
      red: 'text-red-600 dark:text-red-400',
      blue: 'text-blue-600 dark:text-blue-400',
      orange: 'text-orange-600 dark:text-orange-400',
      pink: 'text-pink-600 dark:text-pink-400',
    };
    return colorMap[color] || colorMap.indigo;
  }
}