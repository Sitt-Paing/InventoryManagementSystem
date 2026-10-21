export type SupplierDocumentType = 'purchase-orders' | 'goods-receipts' | 'purchase-returns';

export interface SupplierDocumentItemModel {
    productName: string;
    productSku: string | null;
    uomName: string;
    quantity: number;
    receivedQuantity: number | null;
    unitPrice: number;
    lineAmount: number;
    warehouseName: string | null;
    receiptNo: string | null;
    purchaseOrderNo: string | null;
}

export interface SupplierDocumentModel {
    id: string;
    documentNo: string;
    documentDate: string;
    expectedDate: string | null;
    status: string;
    supplierName: string;
    supplierAddress: string | null;
    warehouseName: string;
    purchaseOrderNo: string | null;
    note: string | null;
    totalItems: number;
    totalAmount: number;
    items: SupplierDocumentItemModel[];
}

export interface SupplierDocumentSummaryModel {
    count: number;
    totalAmount: number;
}

export interface SupplierDashboardModel {
    purchaseOrders: SupplierDocumentSummaryModel;
    goodsReceipts: SupplierDocumentSummaryModel;
    purchaseReturns: SupplierDocumentSummaryModel;
    netReceivedAmount: number;
}

export interface SupplierDocumentFilter {
    pageNumber: number;
    pageSize: number;
    search: string;
    startDate: string | null;
    endDate: string | null;
    sortField: string;
    order: number;
}
