export interface CategoryModel {
  categoryId: number;
  categoryName: string;
  categoryCode: string;
  description?: string;
  totalProducts?: number;
  companyId: string;
  branchId: number;
  isActive: boolean;
}
