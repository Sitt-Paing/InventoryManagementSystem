import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CategoryModel } from '../../core/models/category.model';
import { CategoryService } from '../../core/services/category.service';
import { SharedService } from '../../core/services/shared.service';

import { Table, TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    IconFieldModule,
    InputIconModule,
    DialogModule,
    TagModule
  ],
  templateUrl: './categories.html',
  styleUrl: './categories.scss'
})
export class CategoriesComponent implements OnInit {
  @ViewChild('tblCat') tblCat!: Table;

  categories: CategoryModel[] = [];
  isLoading: boolean = false;
  
  modalVisible: boolean = false;
  dialogHeader: string = '';
  isEditMode: boolean = false;
  catForm!: FormGroup;

  constructor(
    private categoryService: CategoryService,
    public sharedService: SharedService,
    private fb: FormBuilder
  ) {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadCategories();
  }

  initForm(): void {
    this.catForm = this.fb.group({
      categoryId: [0],
      categoryCode: [''],
      categoryName: ['', Validators.required],
      description: ['']
    });
  }

  loadCategories(): void {
    this.isLoading = true;
    this.categoryService.getByCB().subscribe(res => {
      this.categories = res.data;
      this.isLoading = false;
    });
  }

  openCreate(): void {
    this.isEditMode = false;
    this.dialogHeader = 'Add New Category';
    this.catForm.reset({
      categoryId: 0,
      categoryCode: `CAT-${Math.floor(100 + Math.random() * 900)}`
    });
    this.modalVisible = true;
  }

  openEdit(cat: CategoryModel): void {
    this.isEditMode = true;
    this.dialogHeader = `Edit Category: ${cat.categoryCode}`;
    this.catForm.patchValue({
      categoryId: cat.categoryId,
      categoryCode: cat.categoryCode,
      categoryName: cat.categoryName,
      description: cat.description
    });
    this.modalVisible = true;
  }

  saveCategory(): void {
    if (this.catForm.invalid) {
      this.catForm.markAllAsTouched();
      return;
    }
    this.categoryService.save(this.catForm.value).subscribe(() => {
      this.modalVisible = false;
      this.loadCategories();
    });
  }

  deleteCategory(cat: CategoryModel): void {
    if (confirm(`Are you sure you want to delete category "${cat.categoryName}"?`)) {
      this.categoryService.delete(cat.categoryId).subscribe(() => {
        this.loadCategories();
      });
    }
  }
}
