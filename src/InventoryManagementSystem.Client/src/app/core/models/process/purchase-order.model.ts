export interface PurchaseOrderItemModel {
  id?: number;
  purchaseOrderId?: string;
  productId: string;
  productName?: string;
  productSku?: string;
  quantity: number;
  unitPrice: number;
  uomId: number;
  uomName?: string;
  receivedQuantity?: number;
  subTotal?: number;
  createdOn?: Date | null;
  createdBy?: string | null;
  updatedOn?: Date | null;
  updatedBy?: string | null;
}

export interface PurchaseOrderModel {
  id?: string;
  purchaseOrderNo: string;
  supplierId: number;
  supplierName?: string;
  supplierCode?: string;
  warehouseId: number;
  warehouseName?: string;
  orderDate: Date | string;
  expectedDate: Date | string;
  status: boolean;
  totalAmount?: number;
  totalItems?: number;
  items: PurchaseOrderItemModel[];
  createdOn?: Date | null;
  createdBy?: string | null;
  updatedOn?: Date | null;
  updatedBy?: string | null;
}

export interface PurchaseOrderFilterModel {
  q?: string | null;
  orderDate?: string | null;
  supplierId?: number | null;
  warehouseId?: number | null;
  status?: boolean | null;
  sortField?: string | null;
  order?: number | null;
  pageNumber?: number;
  pageSize?: number;
}
