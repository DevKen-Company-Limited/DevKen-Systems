import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';

export interface PhotoViewerData {
  photoUrl: string;
  studentName: string;
  admissionNumber: string;
  additionalInfo?: string;
}

@Component({
  selector: 'app-photo-viewer-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
  ],
  template: `
    <div class="relative flex flex-col max-h-[95vh] bg-gray-900">
      <!-- Header -->
      <div class="flex items-center justify-between px-6 py-4 bg-gradient-to-r from-gray-900 to-gray-800 border-b border-gray-700">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center">
            <mat-icon class="text-white">photo</mat-icon>
          </div>
          <div>
            <h2 class="text-lg font-bold text-white">{{ data.studentName }}</h2>
            <p class="text-sm text-gray-400">{{ data.admissionNumber }}</p>
          </div>
        </div>
        
        <div class="flex items-center gap-2">
          <!-- Zoom Controls -->
          <button 
            mat-icon-button 
            class="text-white hover:bg-gray-700"
            (click)="zoomOut()"
            [disabled]="zoomLevel <= 50"
            matTooltip="Zoom Out">
            <mat-icon>zoom_out</mat-icon>
          </button>
          
          <span class="text-white text-sm font-medium min-w-[60px] text-center">
            {{ zoomLevel }}%
          </span>
          
          <button 
            mat-icon-button 
            class="text-white hover:bg-gray-700"
            (click)="zoomIn()"
            [disabled]="zoomLevel >= 200"
            matTooltip="Zoom In">
            <mat-icon>zoom_in</mat-icon>
          </button>
          
          <button 
            mat-icon-button 
            class="text-white hover:bg-gray-700"
            (click)="resetZoom()"
            matTooltip="Reset Zoom">
            <mat-icon>restart_alt</mat-icon>
          </button>
          
          <div class="w-px h-6 bg-gray-700 mx-2"></div>
          
          <!-- Download Button -->
          <button 
            mat-icon-button 
            class="text-white hover:bg-gray-700"
            (click)="downloadPhoto()"
            matTooltip="Download Photo">
            <mat-icon>download</mat-icon>
          </button>
          
          <!-- Close Button -->
          <button 
            mat-icon-button 
            class="text-white hover:bg-gray-700"
            (click)="close()"
            matTooltip="Close">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      </div>

      <!-- Photo Container -->
      <div 
        #photoContainer
        class="flex-1 overflow-auto bg-gray-900 flex items-center justify-center p-6 cursor-move"
        (mousedown)="onMouseDown($event)"
        (mousemove)="onMouseMove($event)"
        (mouseup)="onMouseUp()"
        (mouseleave)="onMouseUp()"
        (wheel)="onWheel($event)"
        [style.cursor]="isDragging ? 'grabbing' : 'grab'">
        
        <!-- Loading State -->
        <div *ngIf="isLoading" class="text-center">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-4 border-white border-t-transparent mb-4"></div>
          <p class="text-white text-sm">Loading photo...</p>
        </div>

        <!-- Photo -->
        <div 
          *ngIf="!isLoading && !hasError"
          class="relative transition-transform duration-200 ease-out"
          [style.transform]="'scale(' + (zoomLevel / 100) + ') translate(' + translateX + 'px, ' + translateY + 'px)'">
          <img
            [src]="displayUrl"
            [alt]="data.studentName"
            class="max-w-full max-h-[80vh] object-contain rounded-lg shadow-2xl select-none"
            (load)="onImageLoad()"
            (error)="onImageError($event)"
            draggable="false"
          />
        </div>

        <!-- Error State -->
        <div *ngIf="hasError" class="text-center">
          <div class="w-20 h-20 rounded-full bg-red-900/30 flex items-center justify-center mx-auto mb-4">
            <mat-icon class="text-red-400 icon-size-10">error_outline</mat-icon>
          </div>
          <p class="text-white text-lg font-semibold mb-2">Failed to load photo</p>
          <p class="text-gray-400 text-sm">The photo could not be displayed</p>
        </div>
      </div>

      <!-- Footer Info -->
      <div *ngIf="!isLoading && !hasError && data.additionalInfo" 
           class="px-6 py-3 bg-gray-800 border-t border-gray-700">
        <p class="text-sm text-gray-300">{{ data.additionalInfo }}</p>
      </div>

      <!-- Zoom Hint -->
      <div class="absolute bottom-4 left-1/2 transform -translate-x-1/2 px-4 py-2 bg-black/70 rounded-full text-white text-xs pointer-events-none opacity-0 animate-fade-in">
        Use mouse wheel or buttons to zoom • Drag to pan
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }

    @keyframes fade-in {
      from {
        opacity: 0;
        transform: translate(-50%, 10px);
      }
      to {
        opacity: 1;
        transform: translate(-50%, 0);
      }
    }

    .animate-fade-in {
      animation: fade-in 0.3s ease-out forwards;
      animation-delay: 0.5s;
    }

    :host ::ng-deep .mat-mdc-dialog-container {
      padding: 0 !important;
      background: transparent !important;
      box-shadow: none !important;
    }

    :host ::ng-deep .mat-mdc-dialog-surface {
      border-radius: 12px;
      overflow: hidden;
    }
  `]
})
export class PhotoViewerDialogComponent implements OnInit {
  zoomLevel = 100;
  translateX = 0;
  translateY = 0;
  isLoading = true;
  hasError = false;
  
  // Display URL for the image
  displayUrl: string = '';
  
  // Drag and pan state
  isDragging = false;
  dragStartX = 0;
  dragStartY = 0;
  dragStartTranslateX = 0;
  dragStartTranslateY = 0;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: PhotoViewerData,
    private dialogRef: MatDialogRef<PhotoViewerDialogComponent>,
    private sanitizer: DomSanitizer
  ) {
    // Set display URL
    this.displayUrl = this.data.photoUrl;
    console.log('Photo Viewer initialized with URL:', this.displayUrl);
  }

  ngOnInit(): void {
    // Keyboard shortcuts
    document.addEventListener('keydown', this.handleKeyPress.bind(this));
    
    // Debug: Log the photo URL being loaded
    console.log('=== Photo Viewer Debug ===');
    console.log('Photo URL (raw):', this.data.photoUrl);
    console.log('Photo URL type:', typeof this.data.photoUrl);
    console.log('Photo URL starts with blob:', this.data.photoUrl?.startsWith('blob:'));
    console.log('Photo URL starts with http:', this.data.photoUrl?.startsWith('http'));
    console.log('Student:', this.data.studentName);
   // console.log('Safe Photo URL:', this.safePhotoUrl);
    console.log('=========================');
  }

  ngOnDestroy(): void {
    document.removeEventListener('keydown', this.handleKeyPress.bind(this));
  }

  handleKeyPress(event: KeyboardEvent): void {
    switch (event.key) {
      case 'Escape':
        this.close();
        break;
      case '+':
      case '=':
        this.zoomIn();
        break;
      case '-':
      case '_':
        this.zoomOut();
        break;
      case '0':
        this.resetZoom();
        break;
    }
  }

  zoomIn(): void {
    if (this.zoomLevel < 200) {
      this.zoomLevel = Math.min(200, this.zoomLevel + 25);
    }
  }

  zoomOut(): void {
    if (this.zoomLevel > 50) {
      this.zoomLevel = Math.max(50, this.zoomLevel - 25);
      // Reset pan if zoomed out to 100% or less
      if (this.zoomLevel <= 100) {
        this.translateX = 0;
        this.translateY = 0;
      }
    }
  }

  resetZoom(): void {
    this.zoomLevel = 100;
    this.translateX = 0;
    this.translateY = 0;
  }

  onWheel(event: WheelEvent): void {
    event.preventDefault();
    if (event.deltaY < 0) {
      this.zoomIn();
    } else {
      this.zoomOut();
    }
  }

  onMouseDown(event: MouseEvent): void {
    if (this.zoomLevel > 100) {
      this.isDragging = true;
      this.dragStartX = event.clientX;
      this.dragStartY = event.clientY;
      this.dragStartTranslateX = this.translateX;
      this.dragStartTranslateY = this.translateY;
    }
  }

  onMouseMove(event: MouseEvent): void {
    if (this.isDragging) {
      const deltaX = event.clientX - this.dragStartX;
      const deltaY = event.clientY - this.dragStartY;
      this.translateX = this.dragStartTranslateX + deltaX;
      this.translateY = this.dragStartTranslateY + deltaY;
    }
  }

  onMouseUp(): void {
    this.isDragging = false;
  }

  onImageLoad(): void {
    console.log('✅ Photo Viewer - Image loaded successfully');
    console.log('Image dimensions:', event);
    this.isLoading = false;
    this.hasError = false;
  }

  onImageError(event?: any): void {
    console.error('❌ Photo Viewer - Failed to load image');
    console.error('Photo URL:', this.data.photoUrl);
    console.error('Error event:', event);
    this.isLoading = false;
    this.hasError = true;
  }

  downloadPhoto(): void {
    // Create a temporary link to download the photo
    const link = document.createElement('a');
    link.href = this.data.photoUrl;
    link.download = `${this.data.admissionNumber}_${this.data.studentName.replace(/\s+/g, '_')}.jpg`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  close(): void {
    this.dialogRef.close();
  }
}