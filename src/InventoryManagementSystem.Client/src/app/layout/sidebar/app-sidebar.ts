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

  get userName(): string {
    return this.authService.getUserName();
  }

  get userInitial(): string {
    return this.authService.getUserInitial();
  }

  hasRole(item: any): boolean {
    const userRoles = this.authService.getUserRoles();
    const roles = userRoles.length > 0 ? userRoles : ['admin', 'administrator', 'company', 'manager', 'employee'];
    return hasAnyMenuRole(item, roles);
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/auth/login']),
      error: () => this.router.navigate(['/auth/login'])
    });
  }
}
