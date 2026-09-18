export interface StockTransactionModel {
  id?: number;
  productId: string;
  productName?: string;
  productSku?: string;
  productCode?: string;
  userId?: string;
  warehouseId: number;
  warehouseName?: string;
  warehouseLocationId: number;
  warehouseLocationName?: string;
  quantity: number;
  transactionType: 'IN' | 'OUT' | 'ADJUSTMENT';
  transactionDate: Date | string;
  referenceNo?: number | null;
  note?: string | null;
  notes?: string | null;
  createdOn?: Date | string;
  createdBy?: string;
  performedBy?: string;
  updatedOn?: Date | string;
  updatedBy?: string;
  deletedOn?: Date | string;
  deletedBy?: string;

  // Compatibility / Display helpers
  transactionCode?: string;
  unitPrice?: number;
  totalAmount?: number;
}
