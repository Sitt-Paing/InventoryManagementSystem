export enum PurchaseOrderStatus {
  Pending = 0,
  PartiallyReceived = 1,
  Completed = 2,
  Cancelled = 3
}

export const PURCHASE_ORDER_STATUS_OPTIONS = [
  { label: 'Pending', value: PurchaseOrderStatus.Pending },
  { label: 'Partially Received', value: PurchaseOrderStatus.PartiallyReceived },
  { label: 'Completed', value: PurchaseOrderStatus.Completed },
  { label: 'Cancelled', value: PurchaseOrderStatus.Cancelled }
];

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
  status: PurchaseOrderStatus;
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
  status?: PurchaseOrderStatus | null;
  sortField?: string | null;
  order?: number | null;
  pageNumber?: number;
  pageSize?: number;
}
