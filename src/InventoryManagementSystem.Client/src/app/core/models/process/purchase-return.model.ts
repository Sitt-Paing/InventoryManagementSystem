export enum PurchaseReturnStatus {
    Draft = 0,
    Posted = 1
}
export interface PurchaseReturnItemModel {
    id: number;
    purchaseReturnId: string;
    goodReceiptItemId: number;
    goodReceiptId: string;
    receiptNo: string;
    purchaseOrderId: string;
    purchaseOrderNo: string;
    productId: string;
    productName: string;
    uomId: number;
    uomName: string;
    warehouseId: number;
    warehouseName: string;
    receivedQuantity: number;
    returnableQuantity: number;
    returnQuantity: number;
    unitPrice: number;
    lineAmount: number;
}
export interface PurchaseReturnModel {
    id: string;
    returnNo: string;
    supplierId: number;
    supplierName: string;
    returnDate: string;
    status: PurchaseReturnStatus;
    returnReason: string | null;
    postedBy: string | null;
    postedByName: string | null;
    postedOn: string | null;
    createdOn: string | null;
    createdBy: string | null;
    totalItems: number;
    totalAmount: number;
    items: PurchaseReturnItemModel[];
}
export interface ReturnableReceiptItemModel {
    goodReceiptItemId: number;
    goodReceiptId: string;
    receiptNo: string;
    receiptDate: string;
    purchaseOrderNo: string;
    productId: string;
    productName: string;
    uomName: string;
    warehouseId: number;
    warehouseName: string;
    receivedQuantity: number;
    returnedQuantity: number;
    returnableQuantity: number;
}
export interface ReturnDraftLine extends ReturnableReceiptItemModel {
    returnQuantity: number;
}
export interface CreatePurchaseReturnRequest {
    returnNo: string;
    supplierId: number;
    returnDate: string;
    returnReason: string | null;
    idempotencyKey: string;
    items: {
        goodReceiptItemId: number;
        returnQuantity: number;
    }[];
}
export interface UpdatePurchaseReturnRequest extends Omit<CreatePurchaseReturnRequest, 'idempotencyKey' | 'returnNo'> {
    id: string;
}
export interface PurchaseReturnFilter {
    q?: string;
    supplierId?: number;
    status?: PurchaseReturnStatus;
    startDate?: string;
    endDate?: string;
    sortField?: string;
    order?: number;
    pageNumber?: number;
    pageSize?: number;
}
export interface ReturnableItemsFilter {
    supplierId: number;
    q?: string;
    startDate?: string;
    endDate?: string;
    pageNumber?: number;
    pageSize?: number;
}
export interface ReturnPage<T> {
    items: T[];
    totalRecords: number;
    pageNumber: number;
    pageSize: number;
}
export interface ReturnResponse<T> {
    success: boolean;
    message: string;
    data: T;
}
