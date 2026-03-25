import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { BookCopyDto } from 'app/Library/book-copy/Types/book-copy.types';

@Component({
  selector: 'app-book-borrow-item',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './book-borrow-item.component.html',
})
export class BookBorrowItemComponent {
  @Input({ required: true }) copy!: BookCopyDto;
  @Output() onRemove = new EventEmitter<string>();
}