export interface UomCategoryModel {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdOn?: Date | string | null;
  createdBy?: string | null;
  updatedOn?: Date | string | null;
  updatedBy?: string | null;
  deletedOn?: Date | string | null;
  deletedBy?: string | null;
}
