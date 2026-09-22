export interface CategoryModel {
    id: number;
    name: string;
    description?: string | null;
    isActive: boolean;
    createdOn?: Date | null;
    createdBy?: string | null;
    updatedOn?: Date | null;
    updatedBy?: string | null;
    deletedOn?: Date | null;
    deletedBy?: string | null;
}