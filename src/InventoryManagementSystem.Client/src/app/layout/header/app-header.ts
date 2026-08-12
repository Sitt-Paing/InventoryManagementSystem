import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SharedService } from '../../core/services/shared.service';
import { CompanyModel, BranchModel } from '../../core/models/company-branch.model';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TagModule } from 'primeng/tag';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    SelectModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    TagModule,
    MenuModule
  ],
  templateUrl: './app-header.html',
  styleUrl: './app-header.scss'
})
export class AppHeader {
  userMenuItems: MenuItem[] = [
    { label: 'My Profile', icon: 'pi pi-user' },
    { label: 'System Settings', icon: 'pi pi-cog' },
    { separator: true },
    { label: 'Logout', icon: 'pi pi-power-off', styleClass: 'text-red-400' }
  ];

  constructor(public sharedService: SharedService) {}

  onCompanyChange(company: CompanyModel): void {
    if (company) {
      this.sharedService.setCompany(company);
    }
  }

  onBranchChange(branch: BranchModel): void {
    if (branch) {
      this.sharedService.setBranch(branch);
    }
  }
}
