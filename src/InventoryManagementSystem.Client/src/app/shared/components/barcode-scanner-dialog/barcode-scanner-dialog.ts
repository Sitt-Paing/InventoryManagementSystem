import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, ElementRef, EventEmitter, inject, Input, OnChanges, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { TableModule } from 'primeng/table';
import { MessageModule } from 'primeng/message';
import { BarcodeLookupResultModel } from '../../../core/models/master/barcode-lookup.model';
import { ProductService } from '../../../core/services/master/product.service';

@Component({
  selector: 'app-barcode-scanner-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    TagModule,
    TableModule,
    MessageModule,
    CurrencyPipe,
    DatePipe
  ],
  templateUrl: './barcode-scanner-dialog.html',
  styleUrls: ['./barcode-scanner-dialog.scss']
})
export class BarcodeScannerDialog implements OnInit, OnChanges {
  @Input() visible: boolean = false;
  @Input() initialCode: string = '';
  @Output() visibleChange = new EventEmitter<boolean>();

  @ViewChild('barcodeInput') barcodeInputRef?: ElementRef<HTMLInputElement>;

  private productService = inject(ProductService);

  searchCode: string = '';
  isLoading: boolean = false;
  errorMessage: string | null = null;
  lookupResult: BarcodeLookupResultModel | null = null;
  history: string[] = [];

  ngOnInit(): void {
    if (this.initialCode) {
      this.searchCode = this.initialCode;
      this.search();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.errorMessage = null;
      if (this.initialCode && this.initialCode !== this.searchCode) {
        this.searchCode = this.initialCode;
        this.search();
      }
      setTimeout(() => {
        this.barcodeInputRef?.nativeElement?.focus();
        this.barcodeInputRef?.nativeElement?.select();
      }, 150);
    }
  }

  search(): void {
    const code = this.searchCode?.trim();
    if (!code) {
      this.errorMessage = 'Please enter or scan a barcode/SKU.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = null;

    this.productService.barcodeLookup(code).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          this.lookupResult = res.data as BarcodeLookupResultModel;
          this.errorMessage = null;

          // Keep quick history of scanned codes (max 5)
          if (!this.history.includes(code)) {
            this.history = [code, ...this.history.slice(0, 4)];
          }
        } else {
          this.lookupResult = null;
          this.errorMessage = res.message || `No product or location found for '${code}'.`;
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.lookupResult = null;
        this.errorMessage = err.error?.message || `No product or warehouse location found for barcode '${code}'.`;
      }
    });
  }

  clear(): void {
    this.searchCode = '';
    this.lookupResult = null;
    this.errorMessage = null;
    this.barcodeInputRef?.nativeElement?.focus();
  }

  searchFromHistory(code: string): void {
    this.searchCode = code;
    this.search();
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  getStockSeverity(currentStock: number, reorderLevel: number): 'success' | 'warn' | 'danger' {
    if (currentStock <= 0) return 'danger';
    if (currentStock <= reorderLevel) return 'warn';
    return 'success';
  }

  getStockStatusLabel(currentStock: number, reorderLevel: number): string {
    if (currentStock <= 0) return 'Out of Stock';
    if (currentStock <= reorderLevel) return 'Low Stock';
    return 'In Stock';
  }
}
