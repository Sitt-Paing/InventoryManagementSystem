export interface BarcodeLookupResultModel {
  lookupType: 'Product' | 'Location';
  product?: ProductBarcodeLookupModel;
  location?: LocationBarcodeLookupModel;
}

export interface ProductBarcodeLookupModel {
  productId: string;
  productName: string;
  sku?: string;
  barcode?: string;
  brand?: string;
  categoryName?: string;
  unit?: string;
  costPrice: number;
  sellingPrice: number;
  currentStock: number;
  reorderLevel: number;
  reorderQuantity: number;
  status: boolean;
  description?: string;
  locations: ProductLocationDetailModel[];
  warehouseStocks: ProductWarehouseSummaryModel[];
  recentMovements: ProductRecentMovementModel[];
}

export interface ProductLocationDetailModel {
  warehouseId: number;
  warehouseName: string;
  warehouseLocationId?: number;
  locationCode: string;
  zone?: string;
  rack?: string;
  bin?: string;
  locationBarcode?: string;
  quantity: number;
  lastMovementDate?: string | Date;
  isGeneralArea: boolean;
}

export interface ProductWarehouseSummaryModel {
  warehouseId: number;
  warehouseName: string;
  quantity: number;
}

export interface ProductRecentMovementModel {
  id: number;
  transactionType: string;
  quantity: number;
  transactionDate: string | Date;
  warehouseName: string;
  locationCode?: string;
  toWarehouseName?: string;
  toLocationCode?: string;
  userId?: string;
  note?: string;
}

export interface LocationBarcodeLookupModel {
  warehouseLocationId: number;
  warehouseId: number;
  warehouseName: string;
  locationCode: string;
  zone?: string;
  rack?: string;
  bin?: string;
  barcode?: string;
  capacity?: number;
  status: boolean;
  products: LocationProductItemModel[];
}

export interface LocationProductItemModel {
  productId: string;
  productName: string;
  sku?: string;
  barcode?: string;
  unit?: string;
  quantity: number;
  lastMovementDate?: string | Date;
}
