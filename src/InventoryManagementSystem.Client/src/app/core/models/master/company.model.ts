export interface CompanyModel {
  id?: number;
  companyName: string;
  email?: string;
  address?: string;
  contactPerson?: string;
  phone?: string;
  isActive: boolean;
  userCount?: number;
  createdOn?: Date | string;
  createdBy?: string;
  updatedOn?: Date | string;
  updatedBy?: string;
}

export interface RegisterCompanyPayload {
  companyName: string;
  contactPerson?: string;
  companyEmail?: string;
  phone?: string;
  address?: string;
  adminUserName: string;
  adminEmail: string;
  adminPassword: string;
  role?: string;
}
