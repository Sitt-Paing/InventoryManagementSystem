import { CommonModule, CurrencyPipe } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { ProductModel } from '../../../../core/models/master/product.model';
import { Barcode } from '../../../../shared/components/barcode/barcode';

export interface StickerPreset {
  label: string;
  width: number;
  height: number;
}

@Component({
  selector: 'app-product-barcode-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogModule, SelectModule, ButtonModule, CurrencyPipe, Barcode],
  templateUrl: './product-barcode-dialog.html',
})
export class ProductBarcodeDialog implements OnChanges {
  @Input() visible: boolean = false;
  @Input() product: ProductModel | null = null;
  @Input() barcodeValue: string | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();

  stickerWidthMm: number = 50;
  stickerHeightMm: number = 30;

  stickerPresets: StickerPreset[] = [
    { label: '50mm × 30mm (Standard)', width: 50, height: 30 },
    { label: '40mm × 30mm (Small)', width: 40, height: 30 },
    { label: '60mm × 40mm (Medium)', width: 60, height: 40 },
    { label: '70mm × 50mm (Large)', width: 70, height: 50 },
    { label: '80mm × 50mm (Shelf/Bin)', width: 80, height: 50 },
    { label: '100mm × 50mm (Shipping/Box)', width: 100, height: 50 },
  ];

  selectedPreset: StickerPreset = this.stickerPresets[0];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.selectedPreset = this.stickerPresets[0];
      this.stickerWidthMm = this.selectedPreset.width;
      this.stickerHeightMm = this.selectedPreset.height;
    }
  }

  onStickerPresetChange(preset: StickerPreset): void {
    if (preset) {
      this.stickerWidthMm = preset.width;
      this.stickerHeightMm = preset.height;
    }
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  printSticker(): void {
    const stickerEl = document.getElementById('product-barcode-sticker');
    if (!stickerEl) return;

    const printWindow = window.open('', '_blank', 'width=500,height=400');
    if (!printWindow) {
      window.print();
      return;
    }

    const wMm = this.stickerWidthMm || 50;
    const hMm = this.stickerHeightMm || 30;

    printWindow.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>Barcode Sticker</title>
        <style>
          @page {
            size: ${wMm}mm ${hMm}mm;
            margin: 0;
          }
          * { box-sizing: border-box; }
          body {
            margin: 0;
            padding: 0;
            width: ${wMm}mm;
            height: ${hMm}mm;
            display: flex;
            align-items: center;
            justify-content: center;
            background: #fff;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Arial, sans-serif;
          }
          .sticker {
            width: 100%;
            height: 100%;
            padding: 2mm;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            align-items: center;
            overflow: hidden;
          }
          .title-row {
            width: 100%;
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            font-size: 8pt;
            line-height: 1.1;
            margin-bottom: 1mm;
          }
          .prod-name {
            font-weight: 700;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            max-width: 75%;
          }
          .prod-price {
            font-weight: 800;
            font-size: 8pt;
          }
          .sku-brand {
            font-size: 6.5pt;
            color: #555;
            margin-bottom: 0.5mm;
            width: 100%;
          }
          .barcode-wrap {
            flex: 1;
            width: 100%;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow: hidden;
          }
          svg {
            max-width: 100% !important;
            max-height: 100% !important;
          }
        </style>
      </head>
      <body>
        <div class="sticker">
          <div class="title-row">
            <span class="prod-name">${this.product?.name || ''}</span>
            ${this.product?.sellingPrice ? `<span class="prod-price">$${Number(this.product.sellingPrice).toFixed(2)}</span>` : ''}
          </div>
          <div class="sku-brand">
            ${this.product?.sku ? `SKU: ${this.product.sku}` : ''}
            ${this.product?.brand ? ` | ${this.product.brand}` : ''}
          </div>
          <div class="barcode-wrap">
            ${stickerEl.querySelector('.barcode-svg-wrapper')?.innerHTML || ''}
          </div>
        </div>
        <script>
          window.onload = function() {
            window.focus();
            window.print();
            window.close();
          };
        </script>
      </body>
      </html>
    `);
    printWindow.document.close();
  }
}
