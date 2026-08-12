import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { ProductModel } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private products: ProductModel[] = [
    {
      productId: 101,
      productCode: 'PRD-00101',
      name: 'Wireless Ergonomic Keyboard',
      description: 'Ultra-thin Bluetooth mechanical keyboard with RGB backlighting',
      categoryId: 1,
      categoryName: 'Electronics & Gadgets',
      unitPrice: 89.99,
      costPrice: 55.00,
      quantityInStock: 142,
      reorderLevel: 25,
      unitOfMeasure: 'PCS',
      status: 'In Stock',
      companyId: 'COMP001',
      branchId: 1,
      createdAt: '2026-01-15'
    },
    {
      productId: 102,
      productCode: 'PRD-00102',
      name: 'Logitech Master MX 3S Mouse',
      description: 'Precision wireless mouse with quiet clicks and 8K DPI tracking',
      categoryId: 1,
      categoryName: 'Electronics & Gadgets',
      unitPrice: 99.00,
      costPrice: 65.00,
      quantityInStock: 8,
      reorderLevel: 15,
      unitOfMeasure: 'PCS',
      status: 'Low Stock',
      companyId: 'COMP001',
      branchId: 1,
      createdAt: '2026-02-10'
    },
    {
      productId: 103,
      productCode: 'PRD-00103',
      name: 'Dell UltraSharp 27" 4K Monitor',
      description: 'IPS USB-C Hub Monitor with 99% sRGB color coverage',
      categoryId: 1,
      categoryName: 'Electronics & Gadgets',
      unitPrice: 489.00,
      costPrice: 340.00,
      quantityInStock: 35,
      reorderLevel: 10,
      unitOfMeasure: 'UNIT',
      status: 'In Stock',
      companyId: 'COMP001',
      branchId: 1,
      createdAt: '2026-02-22'
    },
    {
      productId: 104,
      productCode: 'PRD-00104',
      name: 'Ergonomic Mesh Executive Chair',
      description: 'High-back mesh chair with adjustable lumbar support and headrest',
      categoryId: 2,
      categoryName: 'Office Furniture',
      unitPrice: 249.50,
      costPrice: 160.00,
      quantityInStock: 0,
      reorderLevel: 5,
      unitOfMeasure: 'PCS',
      status: 'Out of Stock',
      companyId: 'COMP001',
      branchId: 1,
      createdAt: '2026-03-01'
    },
    {
      productId: 105,
      productCode: 'PRD-00105',
      name: 'Standing Desk Converter 36"',
      description: 'Dual monitor riser with gas spring height adjustment',
      categoryId: 2,
      categoryName: 'Office Furniture',
      unitPrice: 179.99,
      costPrice: 110.00,
      quantityInStock: 18,
      reorderLevel: 10,
      unitOfMeasure: 'PCS',
      status: 'In Stock',
      companyId: 'COMP001',
      branchId: 1,
      createdAt: '2026-03-12'
    },
    {
      productId: 106,
      productCode: 'PRD-00106',
      name: 'Thermal Receipt Paper Rolls (50-Pack)',
      description: '80mm x 80mm high quality thermal paper rolls for POS printers',
      categoryId: 3,
      categoryName: 'POS Supplies & Stationery',
      unitPrice: 34.99,
      costPrice: 18.00,
      quantityInStock: 4,
      reorderLevel: 20,
      unitOfMeasure: 'BOX',
      status: 'Low Stock',
      companyId: 'COMP001',
      branchId: 1,
      createdAt: '2026-04-05'
    },
    {
      productId: 107,
      productCode: 'PRD-00107',
      name: '2D Wireless Barcode Scanner',
      description: 'Handheld QR and 1D/2D barcode reader with charging cradle',
      categoryId: 3,
      categoryName: 'POS Supplies & Stationery',
      unitPrice: 75.00,
      costPrice: 42.00,
      quantityInStock: 52,
      reorderLevel: 15,
      unitOfMeasure: 'SET',
      status: 'In Stock',
      companyId: 'COMP001',
      branchId: 1,
      createdAt: '2026-05-18'
    }
  ];

  getByCB(companyId?: string, branchId?: number): Observable<{ data: ProductModel[] }> {
    return of({ data: this.products });
  }

  getById(productId: number): Observable<ProductModel | undefined> {
    const prd = this.products.find(p => p.productId === productId);
    return of(prd);
  }

  save(product: Partial<ProductModel>): Observable<ProductModel> {
    if (product.productId) {
      const index = this.products.findIndex(p => p.productId === product.productId);
      if (index !== -1) {
        this.products[index] = { ...this.products[index], ...product } as ProductModel;
        return of(this.products[index]);
      }
    }
    const newId = Math.max(...this.products.map(p => p.productId), 100) + 1;
    const newProd: ProductModel = {
      productId: newId,
      productCode: `PRD-00${newId}`,
      name: product.name || 'New Product',
      description: product.description || '',
      categoryId: product.categoryId || 1,
      categoryName: product.categoryName || 'General',
      unitPrice: product.unitPrice || 0,
      costPrice: product.costPrice || 0,
      quantityInStock: product.quantityInStock || 0,
      reorderLevel: product.reorderLevel || 10,
      unitOfMeasure: product.unitOfMeasure || 'PCS',
      status: (product.quantityInStock ?? 0) === 0 ? 'Out of Stock' : (product.quantityInStock ?? 0) <= (product.reorderLevel ?? 10) ? 'Low Stock' : 'In Stock',
      companyId: product.companyId || 'COMP001',
      branchId: product.branchId || 1,
      createdAt: new Date().toISOString().slice(0, 10)
    };
    this.products.unshift(newProd);
    return of(newProd);
  }

  delete(productId: number): Observable<boolean> {
    this.products = this.products.filter(p => p.productId !== productId);
    return of(true);
  }
}
