export interface ProductModel {
  id?: string;
  productId?: string | number;
  name: string;
  sku?: string;
  productCode?: string;
  barcode?: string;
  categoryId: number;
  categoryName?: string;
  unitPrice: number;
  costPrice?: number;
  currentStock: number;
  quantityInStock?: number;
  reorderLevel: number;
  unitOfMeasure?: string;
  status?: string;
  createdOn?: Date | string;
  createdAt?: Date | string;
  createdBy?: string;
  updatedOn?: Date | string;
  updatedAt?: Date | string;
  updatedBy?: string;
}
