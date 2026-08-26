import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryModel } from '../../../core/models/category.model';
import { ExportColumnModel } from '../../../core/models/export-column.model';
import { CategoryService } from '../../../core/services/category.service';
import { ExportService } from '../../../core/services/export.service';
import { LoggerService } from '../../../core/services/logger.service';
import { SharedService } from '../../../core/services/shared.service';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { SplitButtonModule } from 'primeng/splitbutton';
import { Table, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ToggleSwitch, ToggleSwitchModule } from 'primeng/toggleswitch';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    CommonModule,
    ToggleSwitchModule,
    SplitButtonModule,
    SelectModule,
    TagModule,
    DialogModule,
    ConfirmDialogModule,
    MessageModule,
    IconFieldModule,
    InputTextModule,
    ButtonModule,
    TableModule,
    ToastModule,
    InputIconModule,
  ],
  providers: [DatePipe, ConfirmationService, ExportService, MessageService],
  templateUrl: './categories.html',
  styleUrl: './categories.scss',
})
export class Categories implements OnInit {
  @ViewChild(Table) tblCategory!: Table;
  categories: CategoryModel[] = [];
  selectedCategory!: CategoryModel;
  errorMessage = signal<any[]>([]);

  items!: MenuItem[];
  modalVisible: boolean = false;
  isEdit: boolean = false;
  status: boolean = false;
  isLoading: boolean = false;
  isSubmitting: boolean = false;

  private formBuilder = inject(FormBuilder);
  public categoryForm = this.formBuilder.group({
    id: [0],
    name: ['', Validators.required],
    description: [''],
    isActive: [true],
    createdOn: [null as any],
    createdBy: [null as any],
    updatedOn: [null],
    updatedBy: [null],
    deletedOn: [null],
    deletedBy: [null],
  });

  constructor(
    private shareService: SharedService,
    private categoryService: CategoryService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private loggerService: LoggerService,
    private datePipe: DatePipe,
    private exportService: ExportService,
    private cdr: ChangeDetectorRef
  ) {
    this.items = [
      {
        label: 'Update',
        icon: 'pi pi-pencil',
        command: () => {
          this.update();
        },
      },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => {
          this.delete();
        },
      },
      {
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.excel(),
      },
    ];
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;

    this.categoryService.get().subscribe({
      next: (res) => {
        this.categories = (res.data || []) as CategoryModel[];
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Category API error', err);
        this.isLoading = false;
      },
    });
  }

  showDialog() {
    this.modalVisible = true;
  }

  create(): void {
    this.isEdit = false;
    this.modalVisible = true;
    this.categoryForm.reset();
    this.errorMessage.set([]);
    this.categoryForm.controls.id.setValue(0);
    this.categoryForm.controls.isActive.setValue(true);
    this.showDialog();
  }

  update(): void {
    this.isEdit = true;
    this.categoryForm.reset();

    if (this.selectedCategory !== null && this.selectedCategory !== undefined) {
      this.categoryForm.controls['id'].setValue(this.selectedCategory.id);
      this.categoryForm.controls['name'].setValue(this.selectedCategory.name);
      this.categoryForm.controls['description'].setValue(this.selectedCategory.description ?? '');
      this.categoryForm.controls['isActive'].setValue(this.selectedCategory.isActive);
      this.categoryForm.controls['createdOn'].setValue(this.selectedCategory.createdOn as any);
      this.categoryForm.controls['createdBy'].setValue(this.selectedCategory.createdBy as any);
      this.showDialog();
      this.selectedCategory = null as any;
      this.errorMessage.set([]);
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please Select Category',
      });
    }
  }

  delete(): void {
    if (this.selectedCategory != null) {
      this.confirmationService.confirm({
        message: 'Are You Sure Want To Delete?',
        header: 'Delete Confirmation',
        icon: 'pi pi-info-circle',
        accept: () => {
          this.categoryService.delete(this.selectedCategory.id).subscribe((res) => {
            this.messageService.add({
              key: 'globalMessage',
              severity: 'success',
              summary: 'Confirmed',
              detail: res.message,
            });
            this.loadData();
            this.selectedCategory = null as any;
            this.cdr.detectChanges();
          });
        },
        reject: () => {
          this.selectedCategory = null as any;
        },
        key: 'positionDialog',
      });
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please Select Category',
      });
    }
  }

  onSubmit(): void {
    if (!this.categoryForm.valid) {
      Object.keys(this.categoryForm.controls).forEach((field) => {
        this.categoryForm.get(field)?.markAsDirty({ onlySelf: true });
      });
      return;
    }

    this.isSubmitting = true;
    let model = this.categoryForm.value as CategoryModel;

    if (!this.isEdit) {
      // model.id = 0;
      model.createdOn = new Date();
      model.createdBy = this.shareService.getUserName() ?? '';
      this.isSubmitting = true;

      this.categoryService.create(model).subscribe({
        next: (res) => {
          if (res.success) {
            this.modalVisible = false;
            this.loadData();
            this.messageService.add({
              key: 'globalMessage',
              severity: 'info',
              summary: 'Success',
              detail: res.message ? res.message.toString() : 'Category created successfully',
            });
            this.cdr.detectChanges();
          }
          this.isSubmitting = false;
        },
        error: () => {
          this.isSubmitting = false;
        },
      });
    } else {
      model.updatedOn = new Date();
      model.updatedBy = this.shareService.getUserName() ?? '';

      this.categoryService.update(model).subscribe({
        next: (res) => {
          if (res.success) {
            this.modalVisible = false;
            this.loadData();
            this.selectedCategory = null as any;
            this.messageService.add({
              key: 'globalMessage',
              severity: 'info',
              summary: 'Success',
              detail: res.message ? res.message.toString() : 'Category updated successfully',
            });
            this.cdr.detectChanges();
          }
          this.isSubmitting = false;
        },
        error: () => {
          this.isSubmitting = false;
        },
      });
    }
  }

  onDialogHide(): void {
    this.selectedCategory = null as any;
    this.modalVisible = false;
  }

  excel(): void {
    this.exportService.excelAll('Category', this.tblCategory);
  }
}
