import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, ElementRef, inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { SupplierDocumentModel, SupplierDocumentType } from '../../../core/models/supplier/supplier-document.model';
import { SupplierDocumentsService } from '../../../core/services/supplier/supplier-documents.service';
import { AuthService } from '../../../core/services/auth.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-supplier-documents',
  imports: [CommonModule, FormsModule, ButtonModule, DatePickerModule, DialogModule, IconFieldModule,
    InputIconModule, InputTextModule, TableModule, TagModule, TranslatePipe],
  templateUrl: './supplier-documents.html',
})
export class SupplierDocuments implements OnInit, OnDestroy {
  @ViewChild('voucher') voucher?: ElementRef<HTMLElement>;
  kind: SupplierDocumentType = 'purchase-orders';
  titleKey: string = 'NAV.PURCHASE_ORDERS';
  documents: SupplierDocumentModel[] = [];
  current: SupplierDocumentModel | null = null;
  isLoading: boolean = false;
  isDetailLoading: boolean = false;
  detailVisible: boolean = false;
  printPreview: boolean = false;
  listError: string = '';
  detailError: string = '';
  searchKeyword: string = '';
  startDate: Date | null = null;
  endDate: Date | null = null;
  first: number = 0;
  rows: number = 20;
  totalRecords: number = 0;
  sortField: string = 'createdOn';
  sortOrder: number = -1;
  private destroyRef = inject(DestroyRef);
  private listRequest?: Subscription;
  private detailRequest?: Subscription;
  private printFrame?: HTMLIFrameElement;

  constructor(
    private documentsService: SupplierDocumentsService,
    private route: ActivatedRoute,
    public authService: AuthService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      this.kind = data['kind'] as SupplierDocumentType;
      this.titleKey = data['titleKey'];
      this.closeDetail();
      this.clearFilters();
    });
  }

  ngOnDestroy(): void {
    this.listRequest?.unsubscribe();
    this.detailRequest?.unsubscribe();
    this.printFrame?.remove();
  }

  loadData(): void {
    this.listRequest?.unsubscribe();
    if (this.startDate && this.endDate && this.startDate > this.endDate) {
      this.listError = 'Start date must not be after end date.';
      this.isLoading = false;
      return;
    }
    this.isLoading = true;
    this.listError = '';
    this.listRequest = this.documentsService.get(this.kind, {
      pageNumber: Math.floor(this.first / this.rows) + 1,
      pageSize: this.rows,
      search: this.searchKeyword.trim(),
      startDate: this.formatDate(this.startDate),
      endDate: this.formatDate(this.endDate),
      sortField: this.sortField,
      order: this.sortOrder,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.documents = res.data.items;
          this.totalRecords = res.data.totalRecords;
        } else {
          this.documents = [];
          this.totalRecords = 0;
          this.listError = res.message || 'Unable to load documents.';
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.documents = [];
        this.totalRecords = 0;
        this.listError = err.error?.message || 'Unable to load documents.';
        this.cdr.markForCheck();
      }
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? 20;
    this.sortField = typeof event.sortField === 'string' ? event.sortField : 'createdOn';
    this.sortOrder = event.sortOrder ?? -1;
    this.loadData();
  }

  applyFilters(): void {
    this.first = 0;
    this.loadData();
  }

  clearFilters(): void {
    this.searchKeyword = '';
    this.startDate = null;
    this.endDate = null;
    this.first = 0;
    this.rows = 20;
    this.sortField = 'createdOn';
    this.sortOrder = -1;
    this.documents = [];
    this.totalRecords = 0;
    this.loadData();
  }

  viewDetail(row: SupplierDocumentModel, printPreview: boolean = false): void {
    this.detailRequest?.unsubscribe();
    this.current = null;
    this.detailError = '';
    this.printPreview = printPreview;
    this.detailVisible = true;
    this.isDetailLoading = true;
    this.detailRequest = this.documentsService.getById(this.kind, row.id)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          this.isDetailLoading = false;
          if (res.success) this.current = res.data;
          else this.detailError = res.message || 'Unable to load document.';
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.isDetailLoading = false;
          this.detailError = err.error?.message || 'Unable to load document.';
          this.cdr.markForCheck();
        }
      });
  }

  closeDetail(): void {
    this.detailRequest?.unsubscribe();
    this.detailVisible = false;
    this.current = null;
    this.detailError = '';
    this.isDetailLoading = false;
    this.printFrame?.remove();
    this.printFrame = undefined;
  }

  printVoucher(): void {
    if (!this.current || !this.voucher) return;
    this.printFrame?.remove();
    const frame = document.createElement('iframe');
    this.printFrame = frame;
    frame.title = 'Supplier document print';
    frame.style.cssText = 'position:fixed;width:0;height:0;right:0;bottom:0;border:0';
    frame.onload = () => {
      frame.contentWindow?.focus();
      frame.contentWindow?.print();
    };
    frame.srcdoc = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>Supplier Document</title>
      <style>
        @page {size:A4 ${this.kind === 'purchase-returns' ? 'landscape' : 'portrait'};margin:14mm}
        * {box-sizing:border-box;color:#000 !important;background:transparent !important}
        body {margin:0;font:10pt Arial,sans-serif;line-height:1.4}
        h2 {font-size:18pt;margin:0 0 8px} h3 {font-size:13pt;margin:6px 0}
        p {margin:3px 0} .document-meta {display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin:18px 0}
        table {width:100%;border-collapse:collapse;font-size:9pt;margin:16px 0}
        th,td {padding:7px;border:1px solid #444;text-align:left;vertical-align:top}
        th {font-weight:bold} .text-right {text-align:right} .text-xs {font-size:8pt}
        .document-total {text-align:right;font-weight:bold;margin-top:12px}
        .overflow-x-auto {overflow:visible} tr {break-inside:avoid} thead {display:table-header-group}
      </style></head><body>${this.voucher.nativeElement.innerHTML}</body></html>`;
    document.body.appendChild(frame);
  }

  private formatDate(date: Date | null): string | null {
    if (!date) return null;
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
