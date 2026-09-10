export interface WarehouseModel {
    id: number;
    warehouseCode: string;
    name: string;
    address?: string | null;
    contactPerson?: string | null;
    phone?: string | null;
    email?: string | null;
    capacity?: number | null;
    status: boolean;
    createdOn?: Date | null;
    createdBy?: string | null;
    updatedOn?: Date | null;
    updatedBy?: string | null;
    deletedOn?: Date | null;
    deletedBy?: string | null;
}
