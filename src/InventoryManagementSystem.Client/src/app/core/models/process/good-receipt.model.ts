export interface GoodReceiptItemModel {
  id?: number;
  goodReceiptId?: string;
  purchaseOrderItemId: number;
  productId: string;
  productName?: string;
  productSku?: string;
  uomId: number;
  uomName?: string;
  receivedQuantity: number;
  createdOn?: Date | null;
  createdBy?: string | null;
  updatedOn?: Date | null;
  updatedBy?: string | null;
}

export interface GoodReceiptModel {
  id?: string;
  receiptNo: string;
  warehouseId: number;
  warehouseName?: string;
  purchaseOrderId: string;
  purchaseOrderNo?: string;
  supplierId: number;
  supplierName?: string;
  supplierCode?: string;
  receiptDate: Date | string;
  status: boolean;
  receivedBy: string;
  note?: string;
  totalItems?: number;
  items: GoodReceiptItemModel[];
  createdOn?: Date | null;
  createdBy?: string | null;
  updatedOn?: Date | null;
  updatedBy?: string | null;
}

export interface GoodReceiptFilterModel {
  q?: string | null;
  receiptDate?: string | null;
  supplierId?: number | null;
  warehouseId?: number | null;
  purchaseOrderId?: string | null;
  status?: boolean | null;
  sortField?: string | null;
  order?: number | null;
  pageNumber?: number;
  pageSize?: number;
}
