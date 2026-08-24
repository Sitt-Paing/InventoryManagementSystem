import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ExportService {
  excel(filename: string, tableElement: any): void {
    if (!tableElement) {
      console.warn('Table element not available for export');
      return;
    }
    
    try {
      const htmlTable = tableElement.nativeElement || tableElement;
      const html = htmlTable.outerHTML;
      const blob = new Blob(['\ufeff', html], {
        type: 'application/vnd.ms-excel'
      });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${filename}_${new Date().toISOString().slice(0, 10)}.xls`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    } catch (e) {
      console.error('Export failed:', e);
    }
  }

  excelAll(filename: string, tableElement: any): void {
    this.excel(filename, tableElement);
  }

  excel_blob(filename: string, blob: Blob): void {
    try {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${filename}_${new Date().toISOString().slice(0, 10)}.xlsx`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
    } catch (e) {
      console.error('Export blob download failed:', e);
    }
  }
}
