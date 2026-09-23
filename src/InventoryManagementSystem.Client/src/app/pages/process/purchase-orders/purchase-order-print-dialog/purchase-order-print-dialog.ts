import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { PurchaseOrderModel } from '../../../../core/models/process/purchase-order.model';

@Component({
  selector: 'app-purchase-order-print-dialog',
  standalone: true,
  imports: [CommonModule, DialogModule, ButtonModule, DatePipe, DecimalPipe],
  templateUrl: './purchase-order-print-dialog.html',
  styleUrl: './purchase-order-print-dialog.scss',
})
export class PurchaseOrderPrintDialog {
  @Input() visible: boolean = false;
  @Input() order: PurchaseOrderModel | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  printVoucher(): void {
    const voucherEl = document.getElementById('printable-po-voucher');
    if (!voucherEl) return;

    let printFrame = document.getElementById('po-print-voucher-frame') as HTMLIFrameElement;
    if (!printFrame) {
      printFrame = document.createElement('iframe');
      printFrame.id = 'po-print-voucher-frame';
      printFrame.style.position = 'fixed';
      printFrame.style.right = '0';
      printFrame.style.bottom = '0';
      printFrame.style.width = '0';
      printFrame.style.height = '0';
      printFrame.style.border = '0';
      document.body.appendChild(printFrame);
    }

    const html = `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <title>Purchase Order - ${this.order?.purchaseOrderNo || ''}</title>
        <style>
          @page {
            size: A4 portrait;
            margin: 12mm 15mm;
          }
          * {
            box-sizing: border-box;
            color: #000000 !important;
            background: transparent !important;
            -webkit-print-color-adjust: exact;
            print-color-adjust: exact;
          }
          html, body {
            height: 100%;
            margin: 0;
            padding: 0;
          }
          body {
            font-family: Arial, Helvetica, sans-serif;
            font-size: 10pt;
            color: #000000;
            background: #ffffff !important;
            line-height: 1.4;
          }
          .voucher-paper {
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            min-height: calc(100vh - 24mm);
            box-sizing: border-box;
          }
          .voucher-content {
            flex: 1 0 auto;
          }
          .voucher-footer {
            margin-top: auto;
            padding-top: 35px;
            padding-bottom: 15mm;
          }
          .header-table {
            width: 100%;
            border-bottom: 2px solid #000000;
            padding-bottom: 8px;
            margin-bottom: 12px;
          }
          .company-title {
            font-size: 16pt;
            font-weight: bold;
            margin: 0;
          }
          .company-sub {
            font-size: 8.5pt;
            margin: 2px 0 0 0;
            text-transform: uppercase;
          }
          .po-title {
            font-size: 15pt;
            font-weight: bold;
            margin: 0;
            text-align: right;
          }
          .po-meta {
            text-align: right;
            font-size: 9.5pt;
            margin: 2px 0 0 0;
          }
          .info-table {
            width: 100%;
            margin: 12px 0 16px 0;
            border-collapse: separate;
            border-spacing: 14px 0;
          }
          .info-box {
            width: 50%;
            border: 1px solid #000000;
            padding: 8px 10px;
            vertical-align: top;
          }
          .info-box-title {
            font-weight: bold;
            font-size: 9pt;
            border-bottom: 1px solid #000000;
            padding-bottom: 3px;
            margin-bottom: 5px;
          }
          .info-box p {
            margin: 2px 0;
            font-size: 9.5pt;
          }
          .items-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 12px;
            margin-bottom: 16px;
            font-size: 9.5pt;
          }
          .items-table th, .items-table td {
            border: 1px solid #000000;
            padding: 6px 8px;
          }
          .items-table th {
            font-weight: bold;
            text-transform: uppercase;
            font-size: 8.5pt;
          }
          .total-row td {
            font-weight: bold;
          }
          .sig-table {
            width: 100%;
            margin-top: 0;
            border-collapse: separate;
            border-spacing: 20px 0;
          }
          .sig-cell {
            width: 33.33%;
            text-align: center;
            vertical-align: top;
          }
          .sig-line {
            border-top: 1px solid #000000;
            margin: 0 auto 5px auto;
            width: 90%;
          }
          .sig-cell p {
            font-weight: bold;
            font-size: 9pt;
            margin: 2px 0;
          }
          .sig-cell small {
            font-size: 8pt;
          }
        </style>
      </head>
      <body>
        <div class="voucher-paper">
          ${voucherEl.innerHTML}
        </div>
      </body>
      </html>
    `;

    const doc = printFrame.contentWindow?.document || printFrame.contentDocument;
    if (doc) {
      doc.open();
      doc.write(html);
      doc.close();
      setTimeout(() => {
        printFrame.contentWindow?.focus();
        printFrame.contentWindow?.print();
      }, 300);
    }
  }
}
