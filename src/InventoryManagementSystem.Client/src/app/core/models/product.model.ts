export interface ProductModel {
  id?: string;
  sku?: string;
  name: string;
  categoryId: number;
  categoryName?: string;
  brand?: string;
  unit?: string;
  barcode?: string;
  costPrice?: number;
  sellingPrice?: number;
  currentStock: number;
  reorderLevel: number;
  reorderQuantity?: number;
  tax?: number;
  status?: boolean;
  description?: string;
  createdOn?: Date | string;
  createdBy?: string;
  updatedOn?: Date | string;
  updatedBy?: string;
  deletedOn?: Date | string;
  deletedBy?: string;
}
