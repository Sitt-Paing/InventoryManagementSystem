export enum StockIssueStatus {
    Draft = 0,
    Posted = 1
}

export interface StockIssueItemModel {
    id: number;
    stockIssueId: string;
    productId: string;
    productName: string;
    productSku: string | null;
    uomId: number;
    uomName: string;
    quantity: number;
}

export interface StockIssueModel {
    id: string;
    issueNo: string;
    issueDate: string;
    warehouseId: number;
    warehouseName: string;
    departmentId: number;
    departmentName: string;
    receivedBy: string;
    note: string | null;
    status: StockIssueStatus;
    postedBy: string | null;
    postedByName: string | null;
    postedOn: string | null;
    createdOn: string | null;
    createdBy: string | null;
    updatedOn: string | null;
    updatedBy: string | null;
    totalItems: number;
    items: StockIssueItemModel[];
}

export interface StockIssueRequest {
    issueNo: string;
    issueDate: string;
    warehouseId: number;
    departmentId: number;
    receivedBy: string;
    note: string | null;
    items: { productId: string; quantity: number }[];
}

export interface UpdateStockIssueRequest extends Omit<StockIssueRequest, 'issueNo'> {
    id: string;
}

export interface StockIssueFilter {
    search?: string;
    warehouseId?: number | null;
    departmentId?: number | null;
    status?: StockIssueStatus | null;
    startDate?: string;
    endDate?: string;
    pageNumber?: number;
    pageSize?: number;
}

export interface StockIssuePage {
    items: StockIssueModel[];
    totalRecords: number;
    pageNumber: number;
    pageSize: number;
}

export interface StockIssueResponse<T> {
    success: boolean;
    message: string;
    data: T;
}

export interface StockIssueDraftLine {
    productId: string;
    productName: string;
    productSku: string | null;
    uomName: string;
    quantity: number | null;
    availableQuantity: number | null;
}
