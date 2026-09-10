export interface WarehouseLocationModel {
    id: number;
    warehouseId: number;
    warehouseName?: string | null;
    warehouseCode?: string | null;
    locationCode: string;
    zone?: string | null;
    rack?: string | null;
    bin?: string | null;
    barcode?: string | null;
    capacity?: number | null;
    status: boolean;
    createdOn?: Date | null;
    createdBy?: string | null;
    updatedOn?: Date | null;
    updatedBy?: string | null;
    deletedOn?: Date | null;
    deletedBy?: string | null;
}
