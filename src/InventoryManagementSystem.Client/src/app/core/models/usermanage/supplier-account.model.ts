export interface CreateSupplierAccountModel {
    supplierId: number;
    userName: string;
    email: string;
    password: string;
}

export interface SupplierAccountModel {
    userId: string;
    userName: string | null;
    email: string | null;
}
