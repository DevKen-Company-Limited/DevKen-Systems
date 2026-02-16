import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { map, catchError, switchMap } from 'rxjs/operators';
import { API_BASE_URL } from 'app/app.config';
import { AuthService } from 'app/core/auth/auth.service';
import { ExportConfig } from 'app/dialog-modals/Student/export-students-dialog.component';
import { StudentDto } from 'app/administration/students/types/studentdto';

export interface SchoolHeaderInfo {
  schoolName: string;
  schoolAddress?: string;
  schoolPhone?: string;
  schoolEmail?: string;
  schoolMotto?: string;
  schoolLogo?: string;
}

interface EnumMaps {
  genderValueToName: Map<number, string>;
  genderNameToValue: Map<string, number>;
  studentStatusValueToName: Map<number, string>;
  studentStatusNameToValue: Map<string, number>;
  cbcLevelValueToName: Map<number, string>;
  cbcLevelNameToValue: Map<string, number>;
}

interface PhotoData {
  studentId: string;
  base64: string;
  mimeType: string;
}

@Injectable({ providedIn: 'root' })
export class StudentExportService {
  private _apiBaseUrl = inject(API_BASE_URL);
  private _http = inject(HttpClient);
  private _authService = inject(AuthService);

  /**
   * Export students to the selected format
   */
  exportStudents(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo: SchoolHeaderInfo,
    enumMaps: EnumMaps
  ): Observable<{ success: boolean; message?: string }> {
    
    // Load photos if needed (PDF/Word only)
    const needsPhotos = (config.format === 'pdf' || config.format === 'word') && config.includePhoto;
    
    const photosObservable = needsPhotos
      ? this.loadStudentPhotos(students)
      : of([]);

    return photosObservable.pipe(
      switchMap(photos => {
        switch (config.format) {
          case 'excel':
            return this.exportToExcel(students, config, schoolInfo, enumMaps);
          case 'pdf':
            return this.exportToPDF(students, config, schoolInfo, enumMaps, photos);
          case 'word':
            return this.exportToWord(students, config, schoolInfo, enumMaps, photos);
          default:
            return of({ success: false, message: 'Unsupported format' });
        }
      })
    );
  }

  /**
   * Load student photos as base64 data
   */
  private loadStudentPhotos(students: StudentDto[]): Observable<PhotoData[]> {
    const studentsWithPhotos = students.filter(s => s.photoUrl);
    
    if (studentsWithPhotos.length === 0) {
      return of([]);
    }

    const photoRequests = studentsWithPhotos.map(student => {
      const url = student.photoUrl!.startsWith('http')
        ? student.photoUrl!
        : `${this._apiBaseUrl}${student.photoUrl}`;

      const token = this._authService.accessToken;
      const headers = token ? new HttpHeaders().set('Authorization', `Bearer ${token}`) : undefined;

      return this._http.get(url, { responseType: 'blob', headers }).pipe(
        map(blob => {
          return new Promise<PhotoData>((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => {
              const base64 = (reader.result as string).split(',')[1];
              resolve({
                studentId: student.id,
                base64: base64,
                mimeType: blob.type
              });
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
          });
        }),
        switchMap(promise => promise),
        catchError(() => of(null))
      );
    });

    return forkJoin(photoRequests).pipe(
      map(results => results.filter((r): r is PhotoData => r !== null))
    );
  }

  /**
   * Export to Excel format
   */
  private exportToExcel(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo: SchoolHeaderInfo,
    enumMaps: EnumMaps
  ): Observable<{ success: boolean; message?: string }> {
    return new Observable(observer => {
      import('exceljs').then(({ Workbook }) => {
        try {
          const workbook = new Workbook();
          const worksheet = workbook.addWorksheet('Students');

          // Get selected columns
          const selectedColumns = config.columns
            .filter(col => col.selected && col.id !== 'photo')
            .map(col => col.id);

          // Define headers
          const headers = config.columns
            .filter(col => col.selected && col.id !== 'photo')
            .map(col => col.label);

          // Add school header if requested
          let startRow = 1;
          if (config.includeSchoolHeader) {
            worksheet.mergeCells('A1:' + String.fromCharCode(64 + headers.length) + '1');
            worksheet.getCell('A1').value = schoolInfo.schoolName;
            worksheet.getCell('A1').font = { size: 16, bold: true };
            worksheet.getCell('A1').alignment = { horizontal: 'center', vertical: 'middle' };
            
            if (schoolInfo.schoolAddress) {
              worksheet.mergeCells('A2:' + String.fromCharCode(64 + headers.length) + '2');
              worksheet.getCell('A2').value = schoolInfo.schoolAddress;
              worksheet.getCell('A2').alignment = { horizontal: 'center' };
            }
            
            startRow = schoolInfo.schoolAddress ? 4 : 3;
          }

          // Add headers
          worksheet.getRow(startRow).values = headers;
          worksheet.getRow(startRow).font = { bold: true };
          worksheet.getRow(startRow).fill = {
            type: 'pattern',
            pattern: 'solid',
            fgColor: { argb: 'FF4F46E5' }
          };
          worksheet.getRow(startRow).font = { color: { argb: 'FFFFFFFF' }, bold: true };

          // Add data
          students.forEach((student, index) => {
            const row = worksheet.getRow(startRow + 1 + index);
            const rowData: any[] = [];

            selectedColumns.forEach(colId => {
              let value: any = '';

              switch (colId) {
                case 'fullName':
                  value = student.fullName || '';
                  break;
                case 'admissionNumber':
                  value = student.admissionNumber || '';
                  break;
                case 'nemisNumber':
                  value = student.nemisNumber || '';
                  break;
                case 'gender':
                  value = this.getEnumName(student.gender, enumMaps.genderValueToName);
                  break;
                case 'dateOfBirth':
                  value = student.dateOfBirth ? new Date(student.dateOfBirth) : '';
                  break;
                case 'cbcLevel':
                  value = this.getEnumName(student.cbcLevel, enumMaps.cbcLevelValueToName);
                  break;
                case 'currentLevel':
                  value = student.currentLevel || '';
                  break;
                case 'studentStatus':
                  value = this.getEnumName(student.studentStatus, enumMaps.studentStatusValueToName);
                  break;
                case 'primaryGuardianName':
                  value = student.primaryGuardianName || '';
                  break;
                case 'primaryGuardianRelationship':
                  value = student.primaryGuardianRelationship || '';
                  break;
                case 'primaryGuardianPhone':
                  value = student.primaryGuardianPhone || '';
                  break;
                case 'primaryGuardianEmail':
                  value = student.primaryGuardianEmail || '';
                  break;
                case 'secondaryGuardianName':
                  value = student.secondaryGuardianName || '';
                  break;
                case 'secondaryGuardianPhone':
                  value = student.secondaryGuardianPhone || '';
                  break;
                case 'county':
                  value = student.county || '';
                  break;
                case 'subCounty':
                  value = student.subCounty || '';
                  break;
                case 'village':
                  value = student.village || '';
                  break;
                case 'isActive':
                  value = student.isActive ? 'Active' : 'Inactive';
                  break;
                case 'enrollmentDate':
                  value = student.enrollmentDate ? new Date(student.enrollmentDate) : '';
                  break;
                case 'schoolName':
                  value = student.schoolName || '';
                  break;
              }

              rowData.push(value);
            });

            row.values = rowData;
          });

          // Auto-size columns
          worksheet.columns.forEach((column: any) => {
            let maxLength = 0;
            column.eachCell?.({ includeEmpty: true }, (cell: any) => {
              const columnLength = cell.value ? cell.value.toString().length : 10;
              if (columnLength > maxLength) {
                maxLength = columnLength;
              }
            });
            column.width = Math.min(maxLength + 2, 50);
          });

          // Generate file
          workbook.xlsx.writeBuffer().then(buffer => {
            this.downloadFile(buffer, `students_export_${this.getTimestamp()}.xlsx`, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');
            observer.next({ success: true });
            observer.complete();
          });

        } catch (error) {
          observer.error({ success: false, message: 'Failed to generate Excel file' });
        }
      }).catch(error => {
        observer.error({ success: false, message: 'Failed to load Excel library' });
      });
    });
  }

  /**
   * Export to PDF format with proper photo handling
   */
  private exportToPDF(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo: SchoolHeaderInfo,
    enumMaps: EnumMaps,
    photos: PhotoData[]
  ): Observable<{ success: boolean; message?: string }> {
    return new Observable(observer => {
      import('pdfmake/build/pdfmake').then(pdfMakeModule => {
        import('pdfmake/build/vfs_fonts').then(vfsFontsModule => {
          try {
            const pdfMake = pdfMakeModule.default || pdfMakeModule;
            const vfsFonts = vfsFontsModule.default || vfsFontsModule;
            pdfMake.vfs = vfsFonts.pdfMake?.vfs || vfsFonts.vfs;

            // Get selected columns (excluding photo as it's handled separately)
            const selectedColumns = config.columns
              .filter(col => col.selected && col.id !== 'photo')
              .map(col => ({ id: col.id, label: col.label }));

            // Calculate column widths - PDF supports max 6-7 columns comfortably in portrait
            const maxColumnsPerRow = config.includePhoto ? 5 : 7;
            const columnsToShow = selectedColumns.slice(0, maxColumnsPerRow);

            // Build photo map
            const photoMap = new Map<string, PhotoData>();
            photos.forEach(p => photoMap.set(p.studentId, p));

            // Document definition
            const docDefinition: any = {
              pageSize: 'A4',
              pageOrientation: 'portrait',
              pageMargins: [40, config.includeSchoolHeader ? 100 : 60, 40, 60],
              header: config.includeSchoolHeader ? (currentPage: number, pageCount: number) => {
                return {
                  columns: [
                    {
                      width: '*',
                      stack: [
                        {
                          text: schoolInfo.schoolName,
                          style: 'header',
                          alignment: 'center',
                          margin: [0, 20, 0, 5]
                        },
                        schoolInfo.schoolAddress ? {
                          text: schoolInfo.schoolAddress,
                          style: 'subheader',
                          alignment: 'center'
                        } : {},
                        schoolInfo.schoolPhone || schoolInfo.schoolEmail ? {
                          text: [
                            schoolInfo.schoolPhone ? `Tel: ${schoolInfo.schoolPhone}` : '',
                            schoolInfo.schoolPhone && schoolInfo.schoolEmail ? ' | ' : '',
                            schoolInfo.schoolEmail ? `Email: ${schoolInfo.schoolEmail}` : ''
                          ].filter(Boolean).join(''),
                          style: 'subheader',
                          alignment: 'center'
                        } : {}
                      ]
                    }
                  ]
                };
              } : undefined,
              footer: (currentPage: number, pageCount: number) => {
                return {
                  columns: [
                    {
                      text: `Generated on ${new Date().toLocaleDateString()}`,
                      alignment: 'left',
                      margin: [40, 0, 0, 0],
                      fontSize: 8,
                      color: '#666666'
                    },
                    {
                      text: `Page ${currentPage} of ${pageCount}`,
                      alignment: 'right',
                      margin: [0, 0, 40, 0],
                      fontSize: 8,
                      color: '#666666'
                    }
                  ]
                };
              },
              content: [],
              styles: {
                header: {
                  fontSize: 18,
                  bold: true,
                  color: '#1F2937'
                },
                subheader: {
                  fontSize: 10,
                  color: '#6B7280'
                },
                tableHeader: {
                  bold: true,
                  fontSize: 9,
                  color: 'white',
                  fillColor: '#4F46E5'
                },
                tableCell: {
                  fontSize: 8
                }
              }
            };

            // Title
            docDefinition.content.push({
              text: 'Student Directory',
              style: 'header',
              alignment: 'center',
              margin: [0, 0, 0, 20]
            });

            // Create student cards/rows
            students.forEach((student, index) => {
              if (index > 0) {
                docDefinition.content.push({ text: '', margin: [0, 10, 0, 0] });
              }

              if (config.includePhoto && photoMap.has(student.id)) {
                // Card layout with photo
                const photo = photoMap.get(student.id)!;
                const studentData: any[] = [];

                columnsToShow.forEach(col => {
                  const value = this.getStudentValue(student, col.id, enumMaps);
                  studentData.push([
                    { text: col.label + ':', bold: true, fontSize: 8, width: 'auto' },
                    { text: value || '—', fontSize: 8 }
                  ]);
                });

                docDefinition.content.push({
                  columns: [
                    {
                      width: 60,
                      image: `data:${photo.mimeType};base64,${photo.base64}`,
                      fit: [50, 50],
                      margin: [0, 0, 10, 0]
                    },
                    {
                      width: '*',
                      table: {
                        widths: ['auto', '*'],
                        body: studentData
                      },
                      layout: 'noBorders'
                    }
                  ],
                  margin: [0, 0, 0, 5]
                });
              } else {
                // Table row without photo
                const rowData: any[] = [];
                columnsToShow.forEach(col => {
                  rowData.push({
                    text: this.getStudentValue(student, col.id, enumMaps) || '—',
                    style: 'tableCell'
                  });
                });

                if (index === 0) {
                  // Add table header for first student
                  const headerRow = columnsToShow.map(col => ({
                    text: col.label,
                    style: 'tableHeader'
                  }));

                  docDefinition.content.push({
                    table: {
                      headerRows: 1,
                      widths: Array(columnsToShow.length).fill('*'),
                      body: [
                        headerRow,
                        rowData
                      ]
                    },
                    layout: {
                      fillColor: (rowIndex: number) => rowIndex === 0 ? '#4F46E5' : (rowIndex % 2 === 0 ? '#F3F4F6' : null)
                    }
                  });
                } else {
                  // Continue table
                  docDefinition.content.push({
                    table: {
                      widths: Array(columnsToShow.length).fill('*'),
                      body: [rowData]
                    },
                    layout: {
                      fillColor: () => index % 2 === 0 ? '#F3F4F6' : null
                    }
                  });
                }
              }
            });

            // Generate PDF
            const pdfDocGenerator = pdfMake.createPdf(docDefinition);
            pdfDocGenerator.download(`students_export_${this.getTimestamp()}.pdf`);

            observer.next({ success: true });
            observer.complete();

          } catch (error) {
            console.error('PDF generation error:', error);
            observer.error({ success: false, message: 'Failed to generate PDF file' });
          }
        });
      }).catch(error => {
        observer.error({ success: false, message: 'Failed to load PDF library' });
      });
    });
  }

  /**
   * Export to Word format with proper column handling and photos
   */
  private exportToWord(
    students: StudentDto[],
    config: ExportConfig,
    schoolInfo: SchoolHeaderInfo,
    enumMaps: EnumMaps,
    photos: PhotoData[]
  ): Observable<{ success: boolean; message?: string }> {
    return new Observable(observer => {
      import('docx').then(docxModule => {
        try {
          const {
            Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
            AlignmentType, WidthType, BorderStyle, ImageRun, HeadingLevel, VerticalAlign
          } = docxModule;
          
          // Type aliases to avoid TypeScript errors
          type TableRowType = InstanceType<typeof TableRow>;
          type TableCellType = InstanceType<typeof TableCell>;

          // Build photo map
          const photoMap = new Map<string, PhotoData>();
          photos.forEach(p => photoMap.set(p.studentId, p));

          // Get selected columns (max 4-5 for Word to fit properly on page)
          const maxColumnsForWord = config.includePhoto ? 3 : 5;
          const selectedColumns = config.columns
            .filter(col => col.selected && col.id !== 'photo')
            .slice(0, maxColumnsForWord)
            .map(col => ({ id: col.id, label: col.label }));

          // US Letter page with 1 inch margins
          const pageWidth = 12240; // 8.5 inches in DXA
          const margins = { top: 1440, right: 1440, bottom: 1440, left: 1440 };
          const contentWidth = pageWidth - margins.left - margins.right; // 9360 DXA

          const sections: any[] = [];
          const content: any[] = [];

          // Header with school info
          if (config.includeSchoolHeader) {
            content.push(
              new Paragraph({
                text: schoolInfo.schoolName,
                heading: HeadingLevel.HEADING_1,
                alignment: AlignmentType.CENTER,
                spacing: { after: 200 }
              })
            );

            if (schoolInfo.schoolAddress) {
              content.push(
                new Paragraph({
                  text: schoolInfo.schoolAddress,
                  alignment: AlignmentType.CENTER,
                  spacing: { after: 100 }
                })
              );
            }

            if (schoolInfo.schoolPhone || schoolInfo.schoolEmail) {
              const contactParts: string[] = [];
              if (schoolInfo.schoolPhone) contactParts.push(`Tel: ${schoolInfo.schoolPhone}`);
              if (schoolInfo.schoolEmail) contactParts.push(`Email: ${schoolInfo.schoolEmail}`);

              content.push(
                new Paragraph({
                  text: contactParts.join(' | '),
                  alignment: AlignmentType.CENTER,
                  spacing: { after: 400 }
                })
              );
            }
          }

          // Document title
          content.push(
            new Paragraph({
              text: 'Student Directory',
              heading: HeadingLevel.HEADING_1,
              alignment: AlignmentType.CENTER,
              spacing: { after: 400 }
            })
          );

          // Border styling
          const border = { style: BorderStyle.SINGLE, size: 1, color: 'CCCCCC' };
          const borders = { top: border, bottom: border, left: border, right: border };
          const cellMargins = { top: 80, bottom: 80, left: 120, right: 120 };

          if (config.includePhoto) {
            // Card-style layout with photos (one student per row)
            students.forEach(student => {
              const photoData = photoMap.get(student.id);
              const tableRows: TableRowType[] = [];

              // Create data rows
              selectedColumns.forEach(col => {
                const value = this.getStudentValue(student, col.id, enumMaps);
                
                tableRows.push(
                  new TableRow({
                    children: [
                      new TableCell({
                        borders,
                        width: { size: 2800, type: WidthType.DXA },
                        margins: cellMargins,
                        children: [
                          new Paragraph({
                            children: [new TextRun({ text: col.label, bold: true })]
                          })
                        ]
                      }),
                      new TableCell({
                        borders,
                        width: { size: 6560 - (photoData ? 1200 : 0), type: WidthType.DXA },
                        margins: cellMargins,
                        children: [
                          new Paragraph({
                            children: [new TextRun({ text: value || '—' })]
                          })
                        ]
                      })
                    ]
                  })
                );
              });

              const tableChildren: TableCellType[] = [];

              // Photo column
              if (photoData) {
                // Determine image type from MIME type
                const imageType = photoData.mimeType.includes('png') ? 'png' : 'jpg';
                
                tableChildren.push(
                  new TableCell({
                    borders,
                    width: { size: 1200, type: WidthType.DXA },
                    margins: cellMargins,
                    verticalAlign: VerticalAlign.CENTER,
                    rowSpan: selectedColumns.length,
                    children: [
                      new Paragraph({
                        alignment: AlignmentType.CENTER,
                        children: [
                          new ImageRun({
                            type: imageType,
                            data: Uint8Array.from(atob(photoData.base64), c => c.charCodeAt(0)),
                            transformation: {
                              width: 80,
                              height: 80
                            }
                          })
                        ]
                      })
                    ]
                  })
                );
              }

              // Data table column
              tableChildren.push(
                new TableCell({
                  borders,
                  width: { size: contentWidth - (photoData ? 1200 : 0), type: WidthType.DXA },
                  margins: { top: 0, bottom: 0, left: 0, right: 0 },
                  children: [
                    new Table({
                      width: { size: contentWidth - (photoData ? 1200 : 0), type: WidthType.DXA },
                      columnWidths: [2800, 6560 - (photoData ? 1200 : 0)],
                      rows: tableRows
                    })
                  ]
                })
              );

              content.push(
                new Table({
                  width: { size: contentWidth, type: WidthType.DXA },
                  columnWidths: photoData ? [1200, contentWidth - 1200] : [contentWidth],
                  rows: [
                    new TableRow({
                      children: tableChildren
                    })
                  ]
                }),
                new Paragraph({ text: '', spacing: { after: 200 } })
              );
            });
          } else {
            // Table layout without photos
            const columnWidths = selectedColumns.map(() => 
              Math.floor(contentWidth / selectedColumns.length)
            );

            // Header row
            const headerRow = new TableRow({
              children: selectedColumns.map((col, i) =>
                new TableCell({
                  borders,
                  width: { size: columnWidths[i], type: WidthType.DXA },
                  margins: cellMargins,
                  shading: { fill: '4F46E5',  color: 'auto' },
                  children: [
                    new Paragraph({
                      children: [
                        new TextRun({
                          text: col.label,
                          bold: true,
                          color: 'FFFFFF'
                        })
                      ]
                    })
                  ]
                })
              )
            });

            // Data rows
            const dataRows = students.map(student =>
              new TableRow({
                children: selectedColumns.map((col, i) =>
                  new TableCell({
                    borders,
                    width: { size: columnWidths[i], type: WidthType.DXA },
                    margins: cellMargins,
                    children: [
                      new Paragraph({
                        children: [
                          new TextRun({
                            text: this.getStudentValue(student, col.id, enumMaps) || '—'
                          })
                        ]
                      })
                    ]
                  })
                )
              })
            );

            content.push(
              new Table({
                width: { size: contentWidth, type: WidthType.DXA },
                columnWidths,
                rows: [headerRow, ...dataRows]
              })
            );
          }

          // Create document
          const doc = new Document({
            sections: [{
              properties: {
                page: {
                  size: {
                    width: pageWidth,
                    height: 15840 // 11 inches
                  },
                  margin: margins
                }
              },
              children: content
            }]
          });

          // Generate file
          Packer.toBlob(doc).then(blob => {
            this.downloadFile(blob, `students_export_${this.getTimestamp()}.docx`, 'application/vnd.openxmlformats-officedocument.wordprocessingml.document');
            observer.next({ success: true });
            observer.complete();
          });

        } catch (error) {
          console.error('Word generation error:', error);
          observer.error({ success: false, message: 'Failed to generate Word file' });
        }
      }).catch(error => {
        observer.error({ success: false, message: 'Failed to load Word library' });
      });
    });
  }

  /**
   * Get enum display name from value
   */
  private getEnumName(value: string | number | undefined, enumMap: Map<number, string>): string {
    if (value === undefined || value === null) return '';
    
    if (typeof value === 'string' && isNaN(Number(value))) {
      return value;
    }
    
    const numValue = typeof value === 'string' ? parseInt(value, 10) : value;
    return enumMap.get(numValue) || value.toString();
  }

  /**
   * Get student field value by column ID
   */
  private getStudentValue(student: StudentDto, columnId: string, enumMaps: EnumMaps): string {
    switch (columnId) {
      case 'fullName':
        return student.fullName || '';
      case 'admissionNumber':
        return student.admissionNumber || '';
      case 'nemisNumber':
        return student.nemisNumber?.toString() || '';
      case 'gender':
        return this.getEnumName(student.gender, enumMaps.genderValueToName);
      case 'dateOfBirth':
        return student.dateOfBirth ? new Date(student.dateOfBirth).toLocaleDateString() : '';
      case 'cbcLevel':
        return this.getEnumName(student.cbcLevel, enumMaps.cbcLevelValueToName);
      case 'currentLevel':
        return student.currentLevel || '';
      case 'studentStatus':
        return this.getEnumName(student.studentStatus, enumMaps.studentStatusValueToName);
      case 'primaryGuardianName':
        return student.primaryGuardianName || '';
      case 'primaryGuardianRelationship':
        return student.primaryGuardianRelationship || '';
      case 'primaryGuardianPhone':
        return student.primaryGuardianPhone || '';
      case 'primaryGuardianEmail':
        return student.primaryGuardianEmail || '';
      case 'secondaryGuardianName':
        return student.secondaryGuardianName || '';
      case 'secondaryGuardianPhone':
        return student.secondaryGuardianPhone || '';
      case 'county':
        return student.county || '';
      case 'subCounty':
        return student.subCounty || '';
      case 'village':
        return student.village || '';
      case 'isActive':
        return student.isActive ? 'Active' : 'Inactive';
      case 'enrollmentDate':
        return student.enrollmentDate ? new Date(student.enrollmentDate).toLocaleDateString() : '';
      case 'schoolName':
        return student.schoolName || '';
      default:
        return '';
    }
  }

  /**
   * Download file helper
   */
  private downloadFile(data: any, filename: string, mimeType: string): void {
    const blob = new Blob([data], { type: mimeType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  /**
   * Get timestamp for filename
   */
  private getTimestamp(): string {
    const now = new Date();
    return `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}_${String(now.getHours()).padStart(2, '0')}${String(now.getMinutes()).padStart(2, '0')}`;
  }
}