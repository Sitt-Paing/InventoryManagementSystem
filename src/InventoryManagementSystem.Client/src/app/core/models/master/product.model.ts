export interface ProductModel {
    id?: string;
    sku?: string;
    name: string;
    categoryId: number;
    categoryName?: string;
    brand?: string;
    unit?: string;
    baseUomId?: number;
    baseUomCode?: string;
    baseUomName?: string;
    purchaseUomId?: number | null;
    purchaseUomCode?: string;
    purchaseUomName?: string;
    saleUomId?: number | null;
    saleUomCode?: string;
    saleUomName?: string;
    barcode?: string;
    costPrice?: number;
    sellingPrice?: number;
    currentStock: number;
    reorderLevel: number;
    reorderQuantity?: number;
    tax?: number;
    status?: boolean;
    description?: string;
    createdOn?: Date | string;
    createdBy?: string;
    updatedOn?: Date | string;
    updatedBy?: string;
    deletedOn?: Date | string;
    deletedBy?: string;
}

export interface ProductListItemModel {
    id: string;
    name: string;
    sellingPrice: number;
}

export interface ProductPagedModel {
    items: ProductModel[];
    totalRecords: number;
    pageNumber: number;
    pageSize: number;
}
export interface ProductFilterModel {
    categoryId?: number | null;
    search?: string | null;
    pageNumber?: number | null;
    pageSize?: number | null;
}

export interface ProductPagedResponseModel {
    statusCode: number;
    success: boolean;
    message: string;
    data: ProductPagedModel;
}
