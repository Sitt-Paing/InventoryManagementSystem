export interface DepartmentsModel {
    id: number;
    deptName: string;
    isActive: boolean;
    createdOn?: Date | null;
    createdBy?: string | null;
    updatedOn?: Date | null;
    updatedBy?: string | null;
    deletedOn?: Date | null;
    deletedBy?: string | null;
}
