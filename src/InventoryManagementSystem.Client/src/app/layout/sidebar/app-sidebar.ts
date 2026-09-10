import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';
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
export class AppSidebar implements OnInit {
  navGroups: NavGroup[] = NAVIGATION_MENU.map(group => ({
    groupName: group.label,
    transKey: group.transKey,
    items: group.items
  }));

  // Track expanded/collapsed state for each menu group
  expandedGroups = signal<Record<string, boolean>>(
    NAVIGATION_MENU.reduce((acc, group) => {
      acc[group.label] = true;
      return acc;
    }, {} as Record<string, boolean>)
  );

  constructor(
    public sharedService: SharedService,
    public translationService: TranslationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.autoExpandActiveGroup(this.router.url);

    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => {
        this.autoExpandActiveGroup(event.urlAfterRedirects || event.url);
      });
  }

  toggleGroup(groupName: string): void {
    this.expandedGroups.update(groups => ({
      ...groups,
      [groupName]: !groups[groupName]
    }));
  }

  isGroupExpanded(groupName: string): boolean {
    const groups = this.expandedGroups();
    return groups[groupName] !== undefined ? groups[groupName] : true;
  }

  private autoExpandActiveGroup(url: string): void {
    for (const group of this.navGroups) {
      const hasActiveChild = group.items.some(
        item => item.routerLink && (url === item.routerLink || url.startsWith(item.routerLink + '/'))
      );
      if (hasActiveChild) {
        this.expandedGroups.update(groups => ({
          ...groups,
          [group.groupName]: true
        }));
      }
    }
  }

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
