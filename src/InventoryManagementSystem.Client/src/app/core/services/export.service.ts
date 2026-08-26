import { DatePipe } from '@angular/common';
import { Injectable } from '@angular/core';
import { saveAs } from 'file-saver';
import * as xlsx from 'xlsx';
import * as ExcelJS from 'exceljs';
import { ExportColumnModel } from '../models/export-column.model';

@Injectable({
  providedIn: 'root'
})
export class ExportService {

  constructor(private datePipe: DatePipe) { }

  public async excelAll(fileName: string, table: any): Promise<void> {
    if (!table) {
      console.warn('Table not provided to excelAll');
      return;
    }

    const isPrimeTable = table.paginator !== undefined;
    const originalPaginator = table.paginator;
    const originalRows = table.rows;
    const originalFirst = table.first;

    if (isPrimeTable) {
      const rowCount = Math.max(table.filteredValue?.length ?? table.value?.length ?? 0, 1);
      table.first = 0;
      table.rows = rowCount;
      table.paginator = false;
      table.cd?.detectChanges?.();
      await this.waitForTableRender();
    }

    try {
      await this.excel(fileName, table);
    } finally {
      if (isPrimeTable) {
        table.paginator = originalPaginator;
        table.rows = originalRows;
        table.first = originalFirst;
        table.cd?.detectChanges?.();
        await this.waitForTableRender();
      }
    }
  }

  public async excel(fileName: string, elementOrTable: any): Promise<void> {
    if (!elementOrTable) {
      return;
    }

    let htmlElement: HTMLElement | null = null;
    if (elementOrTable instanceof HTMLElement) {
      htmlElement = elementOrTable;
    } else if (elementOrTable.nativeElement) {
      htmlElement = elementOrTable.nativeElement;
    } else if (elementOrTable.tableViewChild?.nativeElement) {
      htmlElement = elementOrTable.tableViewChild.nativeElement;
    } else if (elementOrTable.el?.nativeElement) {
      htmlElement = elementOrTable.el.nativeElement.querySelector('table') || elementOrTable.el.nativeElement;
    }

    if (!htmlElement) {
      console.warn('Could not resolve HTML element for export');
      return;
    }

    const worksheet = xlsx.utils.table_to_sheet(htmlElement);
    if (!worksheet['!ref']) {
      return;
    }

    const range = xlsx.utils.decode_range(worksheet['!ref'] as string);
    for (let C = range.s.c; C <= range.e.c; ++C) {
      const address = xlsx.utils.encode_col(C) + '1';
      if (!worksheet[address]) continue;
      if (typeof worksheet[address].v === 'string') {
        worksheet[address].v = worksheet[address].v.toUpperCase();
      }
    }
    const workbook = { Sheets: { data: worksheet }, SheetNames: ['data'] };
    const excelBuffer: any = xlsx.write(workbook, { bookType: 'xlsx', type: 'array' });
    this.saveAsExcelFile(excelBuffer, fileName);
    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  public saveAsExcelFile(buffer: any, fileName: string): void {
    const EXCEL_TYPE = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;charset=UTF-8';
    const EXCEL_EXTENSION = '.xlsx';
    const data: Blob = new Blob([buffer], {
      type: EXCEL_TYPE
    });
    saveAs(data, `${fileName} ${this.datePipe.transform(new Date(), 'dd-MMM-yy')}${EXCEL_EXTENSION}`);
  }

  private async waitForTableRender(): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    await new Promise((resolve) => requestAnimationFrame(resolve));
  }

  public excel_blob(fileName: string, response: Blob): void {
    const EXCEL_EXTENSION = '.xlsx';
    const a = document.createElement('a');
    const objectUrl = URL.createObjectURL(response);
    a.href = objectUrl;
    a.download = `${fileName} ${this.datePipe.transform(new Date(), 'dd-MMM-yy')}${EXCEL_EXTENSION}`;
    a.click();
    URL.revokeObjectURL(objectUrl);
  }

  public exportSelectColsWithDynamicHeader(
    data: any[],
    columns: ExportColumnModel[] | { key: string; value: string }[],
    fileName: string
  ): void {
    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet(fileName);

    this.createHeaders(worksheet, columns);
    this.addDataRows(worksheet, data, columns);

    worksheet.columns = columns.map((col) => {
      const maxDataLength = data.reduce((max, item) => {
        const cellValue = item[col.key] ? item[col.key].toString() : '';
        return Math.max(max, cellValue.length);
      }, col.value.length);

      return {
        key: col.key,
        width: Math.max(15, maxDataLength + 5),
      };
    });

    workbook.xlsx
      .writeBuffer()
      .then((buffer) => {
        const blob = new Blob([buffer], {
          type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        });
        this.saveAsExcelFile(blob, fileName);
      })
      .catch((error) => {
        console.error('Error generating Excel file:', error);
      });
  }

  private createHeaders(
    worksheet: ExcelJS.Worksheet,
    columns: ExportColumnModel[] | { key: string; value: string }[]
  ): void {
    const headerRow = worksheet.addRow(columns.map((col) => col.value));
    headerRow.font = { bold: true };
  }

  private addDataRows(
    worksheet: ExcelJS.Worksheet,
    data: any[],
    columns: ExportColumnModel[] | { key: string; value: string }[]
  ): void {
    data.forEach((item, index) => {
      const rowData = columns.map((col) =>
        col.key === 'no' ? index + 1 : item[col.key] ?? ''
      );
      worksheet.addRow(rowData);
    });
  }
}
