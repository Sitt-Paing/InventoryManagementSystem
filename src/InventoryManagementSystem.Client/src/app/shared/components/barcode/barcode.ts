import { AfterViewInit, Component, ElementRef, Input, ViewChild } from '@angular/core';
import JsBarcode from 'jsbarcode';

@Component({
  selector: 'app-barcode',
  imports: [],
  templateUrl: './barcode.html',
  styleUrl: './barcode.scss',
})
export class Barcode implements AfterViewInit {
  @Input() value: string | null = null;
  @ViewChild('barcode', { static: true }) barcodeElement!: ElementRef<SVGSVGElement>;

  ngAfterViewInit(): void {
    this.generateBarcode();
  }

  generateBarcode(): void {
    if(!this.value) return;

    JsBarcode(this.barcodeElement.nativeElement, this.value, {
      format: 'CODE128',
      lineColor: '#000',
      width: 2,
      height: 70,
      displayValue: false,
    }); 
  }
}
