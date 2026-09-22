export interface SuppliersModel {
    id: number;
    supplierCode: string;
    companyName: string;
    contactPerson?: string | null;
    phone?: string | null;
    email?: string | null;
    address?: string | null;
    paymentTerms: string;
    creditLimit?: number | null;
    status: boolean;
    createdOn?: Date | null;
    createdBy?: string | null;
    updatedOn?: Date | null;
    updatedBy?: string | null;
    deletedOn?: Date | null;
    deletedBy?: string | null;
}