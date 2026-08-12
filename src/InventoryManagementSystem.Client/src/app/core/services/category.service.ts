import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { CategoryModel } from '../models/category.model';

@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private categories: CategoryModel[] = [
    {
      categoryId: 1,
      categoryName: 'Electronics & Gadgets',
      categoryCode: 'CAT-ELEC',
      description: 'Computer peripherals, monitors, audio gear, and electronic devices',
      totalProducts: 3,
      companyId: 'COMP001',
      branchId: 1,
      isActive: true
    },
    {
      categoryId: 2,
      categoryName: 'Office Furniture',
      categoryCode: 'CAT-FURN',
      description: 'Ergonomic chairs, desks, standing risers, and office storage units',
      totalProducts: 2,
      companyId: 'COMP001',
      branchId: 1,
      isActive: true
    },
    {
      categoryId: 3,
      categoryName: 'POS Supplies & Stationery',
      categoryCode: 'CAT-POS',
      description: 'Thermal paper, scanners, label printers, and office stationery',
      totalProducts: 2,
      companyId: 'COMP001',
      branchId: 1,
      isActive: true
    },
    {
      categoryId: 4,
      categoryName: 'Networking Equipment',
      categoryCode: 'CAT-NET',
      description: 'Routers, switches, patch cables, and access points',
      totalProducts: 0,
      companyId: 'COMP001',
      branchId: 1,
      isActive: true
    }
  ];

  getByCB(companyId?: string, branchId?: number): Observable<{ data: CategoryModel[] }> {
    return of({ data: this.categories });
  }

  save(category: Partial<CategoryModel>): Observable<CategoryModel> {
    if (category.categoryId) {
      const idx = this.categories.findIndex(c => c.categoryId === category.categoryId);
      if (idx !== -1) {
        this.categories[idx] = { ...this.categories[idx], ...category } as CategoryModel;
        return of(this.categories[idx]);
      }
    }
    const newId = Math.max(...this.categories.map(c => c.categoryId), 0) + 1;
    const newCat: CategoryModel = {
      categoryId: newId,
      categoryName: category.categoryName || 'New Category',
      categoryCode: category.categoryCode || `CAT-${newId}`,
      description: category.description || '',
      totalProducts: 0,
      companyId: category.companyId || 'COMP001',
      branchId: category.branchId || 1,
      isActive: true
    };
    this.categories.unshift(newCat);
    return of(newCat);
  }

  delete(categoryId: number): Observable<boolean> {
    this.categories = this.categories.filter(c => c.categoryId !== categoryId);
    return of(true);
  }
}
