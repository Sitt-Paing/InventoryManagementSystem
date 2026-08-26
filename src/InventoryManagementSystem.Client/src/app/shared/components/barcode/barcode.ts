import { AfterViewInit, Component, ElementRef, Input, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import JsBarcode from 'jsbarcode';

@Component({
  selector: 'app-barcode',
  imports: [],
  templateUrl: './barcode.html',
  styleUrl: './barcode.scss',
})
export class Barcode implements AfterViewInit, OnChanges {
  @Input() value: string | null = null;
  @ViewChild('barcode', { static: true }) barcodeElement!: ElementRef<SVGSVGElement>;

  ngAfterViewInit(): void {
    this.generateBarcode();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['value']) {
      this.generateBarcode();
    }
  }

  generateBarcode(): void {
    if (!this.value || !this.barcodeElement?.nativeElement) return;

    try {
      JsBarcode(this.barcodeElement.nativeElement, this.value, {
        format: 'CODE128',
        lineColor: '#000',
        width: 2,
        height: 70,
        displayValue: true,
      });
    } catch (e) {
      console.error('Barcode rendering error for value:', this.value, e);
    }
  }
}
