import { ChangeDetectorRef, Component, inject, OnInit, ViewChild } from '@angular/core';
import { ToastModule } from "primeng/toast";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { SplitButtonModule } from "primeng/splitbutton";
import { IconFieldModule } from "primeng/iconfield";
import { InputIconModule } from "primeng/inputicon";
import { Table, TableModule } from "primeng/table";
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { DepartmentsModel } from '../../../core/models/master/departments.model';
import { InputTextModule } from 'primeng/inputtext';
import { CommonModule, DatePipe } from '@angular/common';
import { Tag } from "primeng/tag";
import { DialogModule } from "primeng/dialog";
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToggleSwitchModule } from "primeng/toggleswitch";
import { ButtonModule } from "primeng/button";
import { ExportService } from '../../../core/services/export.service';
import { DepartmentsService } from '../../../core/services/master/departments.service';

@Component({
  selector: 'app-departments',
  imports: [
    ToastModule,
    ReactiveFormsModule,
    FormsModule,
    CommonModule,
    ConfirmDialogModule,
    SplitButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    TableModule,
    Tag,
    DialogModule,
    ToggleSwitchModule,
    ButtonModule
  ],
  providers: [ConfirmationService, MessageService, DatePipe, ExportService],
  templateUrl: './departments.html',
})
export class Departments implements OnInit {
  @ViewChild(Table) tblDepartment!: Table;
  items: MenuItem[] = [];
  isLoading: boolean = false;
  departments: DepartmentsModel[] = [];
  selectedDepartment: DepartmentsModel | null = null;
  isEdit: boolean = false;
  modalVisible: boolean = false;
  isSubmitting: boolean = false;

  private formBuilder = inject(FormBuilder);
  public departmentForm = this.formBuilder.group({
    id: [0],
    deptName: ['', [Validators.required, Validators.maxLength(200)]],
    isActive: [true],
  });

  constructor(
    private departmentsService: DepartmentsService,
    private confirmationService: ConfirmationService,
    private dataPipe: DatePipe,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef,
    private exportService: ExportService,
  ) {
    this.items = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => this.update()
      },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        command: () => this.delete()
      },
      {
        label: 'Excel',
        icon: 'pi pi-file-excel',
        command: () => this.excel()
      }
    ];
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.departmentsService.get().subscribe({
      next: (res) => {
        this.departments = res.data as DepartmentsModel[];
        this.selectedDepartment = null;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.messageService.add({ key: 'globalMessage', severity: 'error', summary: 'Error', detail: err.error?.detail || err.error?.message || 'Unable to load departments.' });
        this.cdr.detectChanges();
      }
    })
  }

  onDialogHide(): void {
    this.modalVisible = false;
    this.selectedDepartment = null;
    this.isEdit = false;
  }

  create(): void {
    this.isEdit = false;
    this.modalVisible = true;
    this.selectedDepartment = null;
    this.departmentForm.reset({ id: 0, deptName: '', isActive: true });
  }

  onSubmit(): void {
    if (this.isSubmitting) return;
    if (this.departmentForm.invalid || !this.departmentForm.controls.deptName.value?.trim()) {
      Object.keys(this.departmentForm.controls).forEach(field => {
        this.departmentForm.get(field)?.markAsDirty({ onlySelf: true });
      });
      return;
    }

    this.isSubmitting = true;
    const model = this.departmentForm.getRawValue() as DepartmentsModel;
    model.deptName = model.deptName.trim();
    const request = this.isEdit
      ? this.departmentsService.update(model)
      : this.departmentsService.create(model);
    request.subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) {
          this.modalVisible = false;
          this.messageService.add({
            key: 'globalMessage',
            severity: 'success',
            summary: 'Success',
            detail: this.isEdit ? 'Department updated successfully.' : 'Department created successfully.'
          });
          this.isEdit = false;
          this.departmentForm.reset();
          this.loadData();
        } else {
          this.messageService.add({ key: 'globalMessage', severity: 'error', summary: 'Error', detail: res.message || 'Unable to save department.' });
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.messageService.add({
          key: 'globalMessage',
          severity: 'error',
          summary: 'Error',
          detail: err.error?.detail || err.error?.message || 'Unable to save department.'
        });
        this.cdr.detectChanges();
      }
    });
  }

  update(): void {
    if (this.selectedDepartment) {
      this.isEdit = true;
      this.departmentForm.reset();
      this.departmentForm.patchValue({
        id: this.selectedDepartment.id,
        deptName: this.selectedDepartment.deptName,
        isActive: this.selectedDepartment.isActive,
      });
      this.modalVisible = true;
    } else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a department.'
      });
    }
  }

  delete(): void {
    if (this.selectedDepartment != null) {
      const departmentId = this.selectedDepartment.id;
      this.confirmationService.confirm({
        message: 'Are you sure to delete this department?',
        header: 'Delete Confirmation',
        icon: 'pi pi-info-circle',
        key: 'positionDialog',
        accept: () => {
          this.departmentsService.delete(departmentId).subscribe({
            next: (res) => {
              this.selectedDepartment = null;
              this.messageService.add({
                key: 'globalMessage',
                severity: 'success',
                summary: 'Confirmed',
                detail: res.message ?? 'Department was deleted successfully.',
              });
              this.loadData();
              this.cdr.detectChanges();
            },
            error: (err) => {
              this.messageService.add({ key: 'globalMessage', severity: 'error', summary: 'Error', detail: err.error?.detail || err.error?.message || 'Unable to delete department.' });
              this.cdr.detectChanges();
            }
          });
        },
        reject: () => {
          this.selectedDepartment = null as any;
        },
      })
    }
    else {
      this.messageService.add({
        key: 'globalMessage',
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please Select a Department',
      });
    }
  }

  excel(): void {
    this.exportService.excelAll('Departments', this.tblDepartment);
  }
}
