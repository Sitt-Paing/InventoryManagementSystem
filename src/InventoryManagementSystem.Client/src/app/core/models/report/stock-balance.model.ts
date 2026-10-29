export interface StockBalanceModel {
    warehouseStockId: number;
    companyId: number | null;
    productId: string;
    sku: string | null;
    productName: string;
    categoryId: number;
    categoryName: string;
    warehouseId: number;
    warehouseCode: string;
    warehouseName: string;
    quantity: number;
    uomName: string;
    uomSymbol: string | null;
    stockCreatedOn: string | null;
    stockCreatedBy: string | null;
    stockUpdatedOn: string | null;
    stockUpdatedBy: string | null;
    stockDeletedOn: string | null;
    stockDeletedBy: string | null;
}

export interface StockBalanceFilterModel {
    search: string;
    startDate: string | null;
    endDate: string | null;
    warehouseId: number | null;
    categoryId: number | null;
    showZeroStock: boolean;
    pageNumber: number;
    pageSize: number;
    sortField: string;
    order: number;
}
