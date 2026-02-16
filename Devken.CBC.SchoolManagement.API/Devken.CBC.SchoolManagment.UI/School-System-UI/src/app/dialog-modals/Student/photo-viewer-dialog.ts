import { Component, Inject, OnInit, OnDestroy, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { HttpClient, HttpHeaders } from '@angular/common/http';

export interface PhotoViewerData {
  photoUrl: string;
  studentName: string;
  admissionNumber: string;
  additionalInfo?: string;
  authToken?: string;
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
    <!-- Root div is now the only container -->
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
          <button mat-icon-button class="text-white hover:bg-gray-700"
            (click)="zoomOut()" [disabled]="zoomLevel <= 50" matTooltip="Zoom Out (-)">
            <mat-icon>zoom_out</mat-icon>
          </button>

          <span class="text-white text-sm font-medium min-w-[60px] text-center">
            {{ zoomLevel }}%
          </span>

          <button mat-icon-button class="text-white hover:bg-gray-700"
            (click)="zoomIn()" [disabled]="zoomLevel >= 200" matTooltip="Zoom In (+)">
            <mat-icon>zoom_in</mat-icon>
          </button>

          <button mat-icon-button class="text-white hover:bg-gray-700"
            (click)="resetZoom()" matTooltip="Reset Zoom (0)">
            <mat-icon>restart_alt</mat-icon>
          </button>

          <div class="w-px h-6 bg-gray-700 mx-2"></div>

          <button mat-icon-button class="text-white hover:bg-gray-700"
            (click)="downloadPhoto()" [disabled]="!displayUrl" matTooltip="Download Photo">
            <mat-icon>download</mat-icon>
          </button>

          <button mat-icon-button class="text-white hover:bg-gray-700"
            (click)="close()" matTooltip="Close (Esc)">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      </div>

      <!-- Image container -->
      <div class="flex-1 overflow-auto bg-gray-900 flex items-center justify-center p-6"
           [class.cursor-grab]="zoomLevel > 100 && !isDragging && displayUrl"
           [class.cursor-grabbing]="isDragging"
           (mousedown)="onMouseDown($event)"
           (mousemove)="onMouseMove($event)"
           (mouseup)="onMouseUp()"
           (mouseleave)="onMouseUp()"
           (wheel)="onWheel($event)">
        
        <!-- Loading -->
        <div *ngIf="isLoading" class="text-center">
          <div class="inline-block animate-spin rounded-full h-12 w-12 border-4 border-white border-t-transparent mb-4"></div>
          <p class="text-white text-sm">Loading photo...</p>
        </div>

        <!-- Image -->
        <div *ngIf="displayUrl && !isLoading && !hasError"
             class="relative transition-transform duration-200 ease-out"
             [style.transform]="'scale(' + (zoomLevel / 100) + ') translate(' + translateX + 'px,' + translateY + 'px)'">
          <img [src]="displayUrl"
               [alt]="data.studentName"
               class="max-w-full max-h-[80vh] object-contain rounded-lg shadow-2xl select-none"
               (load)="onImageLoad()"
               (error)="onImageError()"
               draggable="false"/>
        </div>

        <!-- Error -->
        <div *ngIf="hasError" class="text-center">
          <div class="w-20 h-20 rounded-full bg-red-900/30 flex items-center justify-center mx-auto mb-4">
            <mat-icon class="text-red-400 icon-size-10">error_outline</mat-icon>
          </div>
          <p class="text-white text-lg font-semibold mb-2">Failed to load photo</p>
          <p class="text-gray-400 text-sm mb-2">{{ errorMessage }}</p>
          <button mat-stroked-button class="text-white border-white" (click)="retryLoad()">
            <mat-icon>refresh</mat-icon>
            <span class="ml-2">Retry</span>
          </button>
        </div>
      </div>

      <!-- Additional info -->
      <div *ngIf="displayUrl && !hasError && data.additionalInfo" 
           class="px-6 py-3 bg-gray-800 border-t border-gray-700">
        <p class="text-sm text-gray-300">{{ data.additionalInfo }}</p>
      </div>

      <!-- Hint -->
      <div *ngIf="showHint && displayUrl" 
           class="absolute bottom-4 left-1/2 transform -translate-x-1/2 px-4 py-2 bg-black/70 rounded-full text-white text-xs pointer-events-none animate-fade-in-out">
        Use mouse wheel or buttons to zoom • Drag to pan
      </div>

    </div>
  `,
  styles: [`
    @keyframes fade-in-out {
      0% { opacity: 0; transform: translate(-50%, 10px); }
      10% { opacity: 1; transform: translate(-50%, 0); }
      90% { opacity: 1; transform: translate(-50%, 0); }
      100% { opacity: 0; transform: translate(-50%, 10px); }
    }
    .animate-fade-in-out { animation: fade-in-out 3s ease-out forwards; }

    /* Remove any default padding/shadow from dialog container */
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
export class PhotoViewerDialogComponent implements OnInit, OnDestroy {
  zoomLevel = 100;
  translateX = 0;
  translateY = 0;
  isLoading = true;
  hasError = false;
  showHint = false;
  errorMessage = '';
  displayUrl: SafeUrl | null = null;
  private blobUrl: string | null = null;

  isDragging = false;
  dragStartX = 0;
  dragStartY = 0;
  dragStartTranslateX = 0;
  dragStartTranslateY = 0;

  private keydownListener: ((event: KeyboardEvent) => void) | null = null;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: PhotoViewerData,
    private dialogRef: MatDialogRef<PhotoViewerDialogComponent>,
    private http: HttpClient,
    private sanitizer: DomSanitizer,
    private cdr: ChangeDetectorRef,
    private ngZone: NgZone
  ) {}

  ngOnInit(): void {
    this.keydownListener = this.handleKeyPress.bind(this);
    document.addEventListener('keydown', this.keydownListener);
    this.loadPhotoFromBackend();
  }

  ngOnDestroy(): void {
    if (this.keydownListener) {
      document.removeEventListener('keydown', this.keydownListener);
    }
    if (this.blobUrl) URL.revokeObjectURL(this.blobUrl);
  }

  private loadPhotoFromBackend(): void {
    if (!this.data.photoUrl) {
      this.hasError = true;
      this.isLoading = false;
      this.errorMessage = 'No photo URL provided';
      return;
    }

    this.isLoading = true;
    this.hasError = false;

    let headers = new HttpHeaders();
    if (this.data.authToken) headers = headers.set('Authorization', `Bearer ${this.data.authToken}`);

    this.http.get(this.data.photoUrl, { responseType: 'blob', headers }).subscribe({
      next: (blob: Blob) => this.ngZone.run(() => {
        if (blob.size > 0) {
          this.blobUrl = URL.createObjectURL(blob);
          this.displayUrl = this.sanitizer.bypassSecurityTrustUrl(this.blobUrl);
          this.isLoading = false;
        } else {
          this.hasError = true;
          this.isLoading = false;
          this.errorMessage = 'Received empty image';
        }
        this.cdr.detectChanges();
      }),
      error: (error) => this.ngZone.run(() => {
        this.hasError = true;
        this.isLoading = false;
        if (error.status === 404) this.errorMessage = 'Photo not found';
        else if (error.status === 401 || error.status === 403) this.errorMessage = 'Not authorized';
        else if (error.status === 0) this.errorMessage = 'Cannot connect to server';
        else this.errorMessage = `Server error: ${error.status}`;
        this.cdr.detectChanges();
      })
    });
  }

  onImageLoad(): void {
    this.showHint = true;
    setTimeout(() => this.showHint = false, 3000);
  }

  onImageError(): void {
    this.hasError = true;
    this.isLoading = false;
    this.errorMessage = 'Failed to display image';
  }

  retryLoad(): void {
    if (this.blobUrl) URL.revokeObjectURL(this.blobUrl);
    this.blobUrl = null;
    this.displayUrl = null;
    this.loadPhotoFromBackend();
  }

  handleKeyPress(event: KeyboardEvent): void {
    switch (event.key) {
      case 'Escape': this.close(); break;
      case '+': case '=': event.preventDefault(); this.zoomIn(); break;
      case '-': case '_': event.preventDefault(); this.zoomOut(); break;
      case '0': event.preventDefault(); this.resetZoom(); break;
    }
  }

  zoomIn(): void { this.zoomLevel = Math.min(200, this.zoomLevel + 25); }
  zoomOut(): void {
    this.zoomLevel = Math.max(50, this.zoomLevel - 25);
    if (this.zoomLevel <= 100) { this.translateX = 0; this.translateY = 0; }
  }
  resetZoom(): void { this.zoomLevel = 100; this.translateX = 0; this.translateY = 0; }

  onWheel(event: WheelEvent): void {
    event.preventDefault();
    event.deltaY < 0 ? this.zoomIn() : this.zoomOut();
  }

  onMouseDown(event: MouseEvent): void {
    if (this.zoomLevel > 100 && this.displayUrl) {
      this.isDragging = true;
      this.dragStartX = event.clientX;
      this.dragStartY = event.clientY;
      this.dragStartTranslateX = this.translateX;
      this.dragStartTranslateY = this.translateY;
    }
  }
  onMouseMove(event: MouseEvent): void {
    if (this.isDragging) {
      this.translateX = this.dragStartTranslateX + (event.clientX - this.dragStartX);
      this.translateY = this.dragStartTranslateY + (event.clientY - this.dragStartY);
    }
  }
  onMouseUp(): void { this.isDragging = false; }

  downloadPhoto(): void {
    if (!this.blobUrl) return;
    fetch(this.blobUrl)
      .then(r => r.blob())
      .then(blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${this.data.admissionNumber}_${this.data.studentName.replace(/\s+/g,'_')}.jpg`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
      });
  }

  close(): void { this.dialogRef.close(); }
}
