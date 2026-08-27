import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { SharedService } from '../../core/services/shared.service';
import { AuthService } from '../../core/services/auth.service';
import { TranslationService } from '../../core/services/translation.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { NAVIGATION_MENU, hasAnyMenuRole, NavigationMenuGroup, NavigationMenuItem } from '../../app.menu';

export interface NavItem extends NavigationMenuItem {}

export interface NavGroup {
  groupName: string;
  transKey?: string;
  items: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.scss'
})
export class AppSidebar {
  navGroups: NavGroup[] = NAVIGATION_MENU.map(group => ({
    groupName: group.label,
    transKey: group.transKey,
    items: group.items
  }));

  constructor(
    public sharedService: SharedService,
    public translationService: TranslationService,
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
