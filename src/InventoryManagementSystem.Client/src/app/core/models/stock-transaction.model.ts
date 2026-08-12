export interface StockTransactionModel {
  transactionId: number;
  transactionCode: string;
  productId: number;
  productName: string;
  productCode: string;
  transactionType: 'IN' | 'OUT' | 'ADJUSTMENT';
  quantity: number;
  unitPrice: number;
  totalAmount: number;
  referenceNo?: string;
  notes?: string;
  transactionDate: Date | string;
  performedBy: string;
  companyId: string;
  branchId: number;
}
