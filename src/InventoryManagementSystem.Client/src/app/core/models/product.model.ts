export interface ProductModel {
  productId: number | string;
  productCode: string;
  name: string;
  description?: string;
  categoryId: number;
  categoryName?: string;
  unitPrice: number;
  costPrice: number;
  quantityInStock: number;
  reorderLevel: number;
  unitOfMeasure: string;
  status: 'In Stock' | 'Low Stock' | 'Out of Stock';
  createdAt: Date | string;
  updatedAt?: Date | string;
}
