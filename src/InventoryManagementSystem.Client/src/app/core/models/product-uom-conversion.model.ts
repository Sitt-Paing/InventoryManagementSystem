export interface ProductUomConversionModel {
  id: number;
  productId: string;
  fromUomId: number;
  fromUomCode?: string | null;
  fromUomName?: string | null;
  toUomId: number;
  toUomCode?: string | null;
  toUomName?: string | null;
  conversionFactor: number;
  barcode?: string | null;
  isDefaultPurchase: boolean;
  isDefaultSale: boolean;
  isActive: boolean;
  createdOn?: Date | string | null;
  createdBy?: string | null;
  updatedOn?: Date | string | null;
  updatedBy?: string | null;
  deletedOn?: Date | string | null;
  deletedBy?: string | null;
}
