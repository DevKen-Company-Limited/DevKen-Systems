import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { API_BASE_URL } from 'app/app.config';
import { AuthService } from 'app/core/auth/auth.service';
import { ExportConfig } from 'app/dialog-modals/Student/export-students-dialog.component';

import { Observable, forkJoin, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import * as XLSX from 'xlsx';
import { StudentDto } from 'app/administration/students/types/studentdto';

export interface SchoolHeaderInfo {
  schoolName: string;
  schoolLogo?: string;
  schoolAddress?: string;
  schoolPhone?: string;
  schoolEmail?: string;
  schoolMotto?: string;
}

@Injectable({
  providedIn: 'root'
})
export class StudentExportService {
  private _http = inject(HttpClient);
  private _apiBaseUrl = inject(API_BASE_URL);
  private _authService = inject(AuthService);

  /**
   * Main export function that routes to the appropriate export method
   */
  exportStudents(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo?: SchoolHeaderInfo,
    enumMaps?: any
  ): Observable<{ success: boolean; filename: string }> {
    switch (config.format) {
      case 'excel':
        return this.exportToExcel(students, config, schoolInfo, enumMaps);
      case 'pdf':
        return this.exportToPDF(students, config, schoolInfo, enumMaps);
      case 'word':
        return this.exportToWord(students, config, schoolInfo, enumMaps);
      default:
        return of({ success: false, filename: '' });
    }
  }

  /**
   * Export to Excel (XLSX)
   */
  private exportToExcel(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo?: SchoolHeaderInfo,
    enumMaps?: any
  ): Observable<{ success: boolean; filename: string }> {
    return new Observable(observer => {
      try {
        const workbook = XLSX.utils.book_new();
        const worksheetData: any[] = [];

        // Add school header rows if enabled
        if (config.includeSchoolHeader && schoolInfo) {
          worksheetData.push([schoolInfo.schoolName || 'School Name']);
          if (schoolInfo.schoolAddress) worksheetData.push(['Address: ' + schoolInfo.schoolAddress]);
          if (schoolInfo.schoolPhone) worksheetData.push(['Phone: ' + schoolInfo.schoolPhone]);
          if (schoolInfo.schoolEmail) worksheetData.push(['Email: ' + schoolInfo.schoolEmail]);
          worksheetData.push([]); // Empty row
          worksheetData.push(['Student Report']);
          worksheetData.push(['Generated on: ' + new Date().toLocaleDateString()]);
          worksheetData.push([]); // Empty row
        }

        // Add column headers
        const selectedColumns = config.columns.filter(col => col.selected && col.id !== 'photo');
        const headers = selectedColumns.map(col => col.label);
        worksheetData.push(headers);

        // Add student data rows
        students.forEach(student => {
          const row: any[] = [];
          selectedColumns.forEach(column => {
            let value = (student as any)[column.id];

            // Format enum values
            if (enumMaps) {
              if (column.id === 'gender' && value !== undefined) {
                value = this.getEnumName(value, enumMaps.genderValueToName);
              } else if (column.id === 'cbcLevel' && value !== undefined) {
                value = this.getEnumName(value, enumMaps.cbcLevelValueToName);
              } else if (column.id === 'studentStatus' && value !== undefined) {
                value = this.getEnumName(value, enumMaps.studentStatusValueToName);
              }
            }

            // Format boolean values
            if (column.id === 'isActive') {
              value = value ? 'Active' : 'Inactive';
            }

            // Format dates
            if ((column.id === 'dateOfBirth' || column.id === 'enrollmentDate') && value) {
              value = new Date(value).toLocaleDateString();
            }

            row.push(value ?? '—');
          });
          worksheetData.push(row);
        });

        // Create worksheet and add to workbook
        const worksheet = XLSX.utils.aoa_to_sheet(worksheetData);
        
        // Set column widths
        const columnWidths = selectedColumns.map(col => {
          if (col.id === 'fullName') return { wch: 30 };
          if (col.id === 'primaryGuardianEmail') return { wch: 35 };
          return { wch: 20 };
        });
        worksheet['!cols'] = columnWidths;

        XLSX.utils.book_append_sheet(workbook, worksheet, 'Students');

        // Generate filename and download
        const filename = `students_export_${new Date().getTime()}.xlsx`;
        XLSX.writeFile(workbook, filename);

        observer.next({ success: true, filename });
        observer.complete();
      } catch (error) {
        observer.error(error);
      }
    });
  }

  /**
   * Export to PDF
   */
  private exportToPDF(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo?: SchoolHeaderInfo,
    enumMaps?: any
  ): Observable<{ success: boolean; filename: string }> {
    // This would typically call a backend endpoint that uses a library like PDFKit or similar
    // For now, we'll create a simple implementation using the browser's print functionality
    return new Observable(observer => {
      try {
        const selectedColumns = config.columns.filter(col => col.selected);
        
        // Load photos if needed
        const photoLoaders: Observable<{ id: string; dataUrl: string }>[] = [];
        
        if (config.includePhoto) {
          students.forEach(student => {
            if (student.photoUrl) {
              photoLoaders.push(this.loadPhotoAsDataUrl(student.id, student.photoUrl));
            }
          });
        }

        const photoMap = new Map<string, string>();
        
        (photoLoaders.length > 0 ? forkJoin(photoLoaders) : of([])).subscribe({
          next: (photos) => {
            photos.forEach(photo => photoMap.set(photo.id, photo.dataUrl));

            const htmlContent = this.generatePDFHTML(students, config, schoolInfo, enumMaps, photoMap, selectedColumns);
            
            // Create a hidden iframe to print
            const iframe = document.createElement('iframe');
            iframe.style.display = 'none';
            document.body.appendChild(iframe);
            
            const iframeDoc = iframe.contentWindow?.document;
            if (iframeDoc) {
              iframeDoc.open();
              iframeDoc.write(htmlContent);
              iframeDoc.close();
              
              setTimeout(() => {
                iframe.contentWindow?.print();
                setTimeout(() => document.body.removeChild(iframe), 1000);
              }, 500);
            }

            const filename = `students_export_${new Date().getTime()}.pdf`;
            observer.next({ success: true, filename });
            observer.complete();
          },
          error: (err) => observer.error(err)
        });
      } catch (error) {
        observer.error(error);
      }
    });
  }

  /**
   * Export to Word (DOCX)
   */
  private exportToWord(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo?: SchoolHeaderInfo,
    enumMaps?: any
  ): Observable<{ success: boolean; filename: string }> {
    // This would call a backend API endpoint that generates the DOCX file
    // For demonstration, we'll download the data as HTML which can be opened in Word
    return new Observable(observer => {
      try {
        const selectedColumns = config.columns.filter(col => col.selected);
        
        const photoLoaders: Observable<{ id: string; dataUrl: string }>[] = [];
        
        if (config.includePhoto) {
          students.forEach(student => {
            if (student.photoUrl) {
              photoLoaders.push(this.loadPhotoAsDataUrl(student.id, student.photoUrl));
            }
          });
        }

        const photoMap = new Map<string, string>();
        
        (photoLoaders.length > 0 ? forkJoin(photoLoaders) : of([])).subscribe({
          next: (photos) => {
            photos.forEach(photo => photoMap.set(photo.id, photo.dataUrl));

            const htmlContent = this.generateWordHTML(students, config, schoolInfo, enumMaps, photoMap, selectedColumns);
            
            // Create blob and download
            const blob = new Blob([htmlContent], { type: 'application/msword' });
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            const filename = `students_export_${new Date().getTime()}.doc`;
            link.download = filename;
            link.click();
            window.URL.revokeObjectURL(url);

            observer.next({ success: true, filename });
            observer.complete();
          },
          error: (err) => observer.error(err)
        });
      } catch (error) {
        observer.error(error);
      }
    });
  }

  /**
   * Generate HTML for PDF export
   */
  private generatePDFHTML(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo: SchoolHeaderInfo | undefined,
    enumMaps: any,
    photoMap: Map<string, string>,
    selectedColumns: any[]
  ): string {
    let html = `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <title>Students Export</title>
  <style>
    @media print {
      @page { margin: 0.5in; size: landscape; }
      body { margin: 0; }
    }
    body {
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      font-size: 11pt;
      color: #333;
      line-height: 1.4;
    }
    .header {
      text-align: center;
      margin-bottom: 30px;
      padding-bottom: 20px;
      border-bottom: 3px solid #4F46E5;
    }
    .header .logo {
      max-width: 100px;
      max-height: 100px;
      margin-bottom: 10px;
    }
    .header .school-name {
      font-size: 24pt;
      font-weight: bold;
      color: #4F46E5;
      margin: 10px 0;
    }
    .header .school-info {
      font-size: 10pt;
      color: #666;
      margin: 5px 0;
    }
    .header .motto {
      font-style: italic;
      color: #888;
      margin-top: 10px;
    }
    .report-title {
      font-size: 18pt;
      font-weight: bold;
      text-align: center;
      margin: 20px 0;
      color: #1F2937;
    }
    .report-meta {
      text-align: center;
      font-size: 10pt;
      color: #666;
      margin-bottom: 30px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 20px;
      font-size: 9pt;
    }
    th {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 12px 8px;
      text-align: left;
      font-weight: 600;
      border: 1px solid #4F46E5;
    }
    td {
      padding: 10px 8px;
      border: 1px solid #ddd;
      vertical-align: middle;
    }
    tr:nth-child(even) {
      background-color: #f9fafb;
    }
    .photo-cell {
      text-align: center;
    }
    .student-photo {
      width: 40px;
      height: 40px;
      border-radius: 8px;
      object-fit: cover;
      border: 2px solid #e5e7eb;
    }
    .footer {
      margin-top: 40px;
      text-align: center;
      font-size: 9pt;
      color: #999;
      border-top: 1px solid #ddd;
      padding-top: 20px;
    }
  </style>
</head>
<body>`;

    // School header
    if (config.includeSchoolHeader && schoolInfo) {
      html += `<div class="header">`;
      if (schoolInfo.schoolLogo) {
        html += `<img src="${schoolInfo.schoolLogo}" alt="School Logo" class="logo">`;
      }
      html += `<div class="school-name">${schoolInfo.schoolName || 'School Name'}</div>`;
      if (schoolInfo.schoolAddress) {
        html += `<div class="school-info">${schoolInfo.schoolAddress}</div>`;
      }
      if (schoolInfo.schoolPhone || schoolInfo.schoolEmail) {
        html += `<div class="school-info">`;
        if (schoolInfo.schoolPhone) html += `Tel: ${schoolInfo.schoolPhone}`;
        if (schoolInfo.schoolPhone && schoolInfo.schoolEmail) html += ` | `;
        if (schoolInfo.schoolEmail) html += `Email: ${schoolInfo.schoolEmail}`;
        html += `</div>`;
      }
      if (schoolInfo.schoolMotto) {
        html += `<div class="motto">${schoolInfo.schoolMotto}</div>`;
      }
      html += `</div>`;
    }

    // Report title and meta
    html += `
      <div class="report-title">Student Report</div>
      <div class="report-meta">
        Generated on ${new Date().toLocaleDateString()} at ${new Date().toLocaleTimeString()} | 
        Total Students: ${students.length}
      </div>
    `;

    // Table
    html += `<table><thead><tr>`;
    selectedColumns.forEach(col => {
      html += `<th>${col.label}</th>`;
    });
    html += `</tr></thead><tbody>`;

    students.forEach(student => {
      html += `<tr>`;
      selectedColumns.forEach(col => {
        if (col.id === 'photo' && config.includePhoto) {
          const photoData = photoMap.get(student.id);
          html += `<td class="photo-cell">`;
          if (photoData) {
            html += `<img src="${photoData}" alt="${student.fullName}" class="student-photo">`;
          } else {
            html += `<div style="width: 40px; height: 40px; background: #e5e7eb; border-radius: 8px; display: inline-flex; align-items: center; justify-content: center; font-weight: bold; color: #6366f1;">${student.firstName?.[0] || ''}${student.lastName?.[0] || ''}</div>`;
          }
          html += `</td>`;
        } else {
          let value = (student as any)[col.id];
          
          // Format values
          if (enumMaps) {
            if (col.id === 'gender') value = this.getEnumName(value, enumMaps.genderValueToName);
            if (col.id === 'cbcLevel') value = this.getEnumName(value, enumMaps.cbcLevelValueToName);
            if (col.id === 'studentStatus') value = this.getEnumName(value, enumMaps.studentStatusValueToName);
          }
          if (col.id === 'isActive') value = value ? 'Active' : 'Inactive';
          if ((col.id === 'dateOfBirth' || col.id === 'enrollmentDate') && value) {
            value = new Date(value).toLocaleDateString();
          }
          
          html += `<td>${value ?? '—'}</td>`;
        }
      });
      html += `</tr>`;
    });

    html += `</tbody></table>`;
    html += `<div class="footer">This document was generated electronically and is valid without signature.</div>`;
    html += `</body></html>`;

    return html;
  }

  /**
   * Generate HTML for Word export (similar to PDF but with Word-compatible styling)
   */
  private generateWordHTML(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo: SchoolHeaderInfo | undefined,
    enumMaps: any,
    photoMap: Map<string, string>,
    selectedColumns: any[]
  ): string {
    // Similar structure to PDF but with Word-compatible HTML
    return this.generatePDFHTML(students, config, schoolInfo, enumMaps, photoMap, selectedColumns);
  }

  /**
   * Load photo as base64 data URL for embedding in documents
   */
  private loadPhotoAsDataUrl(studentId: string, photoUrl: string): Observable<{ id: string; dataUrl: string }> {
    const url = photoUrl.startsWith('http') ? photoUrl : `${this._apiBaseUrl}${photoUrl}`;
    const token = this._authService.accessToken;
    const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;

    return this._http.get(url, { responseType: 'blob', headers }).pipe(
      map(blob => {
        return new Observable<{ id: string; dataUrl: string }>(observer => {
          const reader = new FileReader();
          reader.onloadend = () => {
            observer.next({ id: studentId, dataUrl: reader.result as string });
            observer.complete();
          };
          reader.onerror = () => observer.error('Failed to read photo');
          reader.readAsDataURL(blob);
        });
      }),
      catchError(() => of(new Observable<{ id: string; dataUrl: string }>(observer => {
        observer.next({ id: studentId, dataUrl: '' });
        observer.complete();
      }))),
      map(obs => {
        let result: { id: string; dataUrl: string } = { id: studentId, dataUrl: '' };
        obs.subscribe(data => result = data);
        return result;
      })
    );
  }

  /**
   * Helper to get enum name from value
   */
  private getEnumName(value: any, enumMap: Map<number, string>): string {
    if (value === undefined || value === null) return '—';
    if (typeof value === 'string' && isNaN(Number(value))) return value;
    const numValue = typeof value === 'string' ? parseInt(value, 10) : value;
    return enumMap.get(numValue) || value.toString();
  }
}