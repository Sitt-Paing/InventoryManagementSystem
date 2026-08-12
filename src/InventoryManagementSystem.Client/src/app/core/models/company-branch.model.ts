export interface CompanyModel {
  companyId: string;
  companyName: string;
  code: string;
}

export interface BranchModel {
  branchId: number;
  companyId: string;
  branchName: string;
  location: string;
}
