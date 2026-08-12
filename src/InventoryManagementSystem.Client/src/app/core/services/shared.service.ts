import { Injectable, signal } from '@angular/core';
import { CompanyModel, BranchModel } from '../models/company-branch.model';

@Injectable({
  providedIn: 'root'
})
export class SharedService {
  private readonly defaultCompanyKey = 'DEFAULT_COMPANY_ID';
  private readonly defaultBranchKey = 'DEFAULT_BRANCH_ID';

  companies = signal<CompanyModel[]>([
    { companyId: 'COMP001', companyName: 'Efficient Soft HQ', code: 'ESHQ' },
    { companyId: 'COMP002', companyName: 'Efficient Tech Global', code: 'ETG' }
  ]);

  branches = signal<BranchModel[]>([
    { branchId: 1, companyId: 'COMP001', branchName: 'Main Warehouse - Yangon', location: 'Yangon Central' },
    { branchId: 2, companyId: 'COMP001', branchName: 'Mandalay Hub', location: 'Mandalay City' },
    { branchId: 3, companyId: 'COMP002', branchName: 'Singapore Logistics Center', location: 'Jurong West' }
  ]);

  selectedCompany = signal<CompanyModel>(this.companies()[0]);
  selectedBranch = signal<BranchModel>(this.branches()[0]);
  sidebarCollapsed = signal<boolean>(false);
  isDarkMode = signal<boolean>(true);

  constructor() {
    const savedComp = localStorage.getItem(this.defaultCompanyKey);
    const savedBranch = localStorage.getItem(this.defaultBranchKey);

    if (savedComp) {
      const comp = this.companies().find(c => c.companyId === savedComp);
      if (comp) this.selectedCompany.set(comp);
    }
    if (savedBranch) {
      const br = this.branches().find(b => b.branchId === Number.parseInt(savedBranch));
      if (br) this.selectedBranch.set(br);
    }
  }

  getDefaultCompany(): string {
    return this.selectedCompany().companyId;
  }

  getDefaultBranch(): string {
    return this.selectedBranch().branchId.toString();
  }

  setCompany(company: CompanyModel): void {
    this.selectedCompany.set(company);
    localStorage.setItem(this.defaultCompanyKey, company.companyId);
  }

  setBranch(branch: BranchModel): void {
    this.selectedBranch.set(branch);
    localStorage.setItem(this.defaultBranchKey, branch.branchId.toString());
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(val => !val);
  }

  toggleDarkMode(): void {
    this.isDarkMode.update(val => !val);
  }
}
