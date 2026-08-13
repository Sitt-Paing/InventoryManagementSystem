import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { SharedService } from '../../core/services/shared.service';
import { AuthService } from '../../core/services/auth.service';
import { NAVIGATION_MENU, hasAnyMenuRole } from '../../app.menu';

export interface NavItem {
  label: string;
  icon: string;
  routerLink?: string | string[];
  badge?: string;
  badgeSeverity?: string;
  data?: any;
  items?: NavItem[];
}

export interface NavGroup {
  groupName: string;
  items: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.scss'
})
export class AppSidebar {
  navGroups: NavGroup[] = NAVIGATION_MENU.map(group => ({
    groupName: group.label,
    items: group.items
  }));

  constructor(
    public sharedService: SharedService,
    private authService: AuthService,
    private router: Router
  ) {}

  hasRole(item: any): boolean {
    const user = this.authService.getCurrentUser();
    const userRoles = (user?.roles && user.roles.length > 0) ? user.roles : ['admin', 'administrator', 'company', 'manager', 'employee'];
    return hasAnyMenuRole(item, userRoles);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
