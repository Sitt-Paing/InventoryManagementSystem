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
  @Input() width: number = 2.0;
  @Input() height: number = 55;
  @Input() fontSize: number = 14;
  @Input() displayValue: boolean = true;
  @ViewChild('barcode', { static: true }) barcodeElement!: ElementRef<SVGSVGElement>;

  ngAfterViewInit(): void {
    this.generateBarcode();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['value'] || changes['width'] || changes['height'] || changes['fontSize'] || changes['displayValue']) {
      this.generateBarcode();
    }
  }

  generateBarcode(): void {
    if (!this.value || !this.barcodeElement?.nativeElement) return;

    try {
      JsBarcode(this.barcodeElement.nativeElement, this.value, {
        format: 'CODE128',
        lineColor: '#000',
        width: this.width,
        height: this.height,
        fontSize: this.fontSize,
        displayValue: this.displayValue,
        margin: 4,
      });
    } catch (e) {
      console.error('Barcode rendering error for value:', this.value, e);
    }
  }
}
