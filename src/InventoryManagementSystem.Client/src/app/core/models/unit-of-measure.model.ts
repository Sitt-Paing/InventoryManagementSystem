export interface UnitOfMeasureModel {
  id: number;
  categoryId: number;
  categoryName?: string | null;
  code: string;
  name: string;
  symbol?: string | null;
  decimalPlaces: number;
  isActive: boolean;
  createdOn?: Date | string | null;
  createdBy?: string | null;
  updatedOn?: Date | string | null;
  updatedBy?: string | null;
  deletedOn?: Date | string | null;
  deletedBy?: string | null;
}
