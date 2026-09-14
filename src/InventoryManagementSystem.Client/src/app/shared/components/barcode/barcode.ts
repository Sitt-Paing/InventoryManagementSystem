import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  ViewChild
} from '@angular/core';
import JsBarcode from 'jsbarcode';

@Component({
  selector: 'app-barcode',
  templateUrl: './barcode.html',
  styleUrl: './barcode.scss',
})
export class Barcode implements AfterViewInit, OnChanges {
  @Input() value: string | null = null;
  @Input() format: string = 'EAN13';
  @Input() width: number = 2;
  @Input() height: number = 55;
  @Input() fontSize: number = 14;
  @Input() displayValue: boolean = true;

  @ViewChild('barcode', { static: true })
  barcodeElement!: ElementRef<SVGSVGElement>;

  ngAfterViewInit(): void {
    this.render();
  }

  ngOnChanges(): void {
    this.render();
  }

  private render(): void {
    const el = this.barcodeElement?.nativeElement;
    if (!el || !this.value) {
      if (el) el.innerHTML = '';
      return;
    }

    const text = this.value.trim();
    if (!text) {
      el.innerHTML = '';
      return;
    }

    try {
      el.innerHTML = '';
      JsBarcode(el, text, {
        format: this.format,
        lineColor: '#000',
        width: this.width,
        height: this.height,
        fontSize: this.fontSize,
        displayValue: this.displayValue,
        margin: 4,
      });
    } catch (err) {
      // If EAN13 fails (e.g. alphanumeric SKU like PRD-001), fallback to CODE128
      try {
        JsBarcode(el, text, {
          format: 'CODE128',
          lineColor: '#000',
          width: this.width,
          height: this.height,
          fontSize: this.fontSize,
          displayValue: this.displayValue,
          margin: 4,
        });
      } catch (_) {
        el.innerHTML = '';
      }
    }
  }
}