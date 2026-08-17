import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, NavigationEnd, Event } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { SharedService } from '../../core/services/shared.service';
import { AuthService } from '../../core/services/auth.service';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { NAVIGATION_MENU } from '../../app.menu';

export interface BreadcrumbInfo {
  parent: string;
  child: string;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    MenuModule
  ],
  templateUrl: './app-header.html',
  styleUrl: './app-header.scss'
})
export class AppHeader implements OnInit, OnDestroy {
  breadcrumb: BreadcrumbInfo = { parent: 'Master', child: 'Inventory' };
  private routerSub!: Subscription;

  userMenuItems: MenuItem[] = [
    { label: 'My Profile', icon: 'pi pi-user' },
    { label: 'System Settings', icon: 'pi pi-cog' },
    { separator: true },
    {
      label: 'Logout',
      icon: 'pi pi-power-off',
      styleClass: 'text-red-500',
      command: () => this.logout()
    }
  ];

  constructor(
    public sharedService: SharedService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.updateBreadcrumb(this.router.url);
    this.routerSub = this.router.events.pipe(
      filter((e: Event): e is NavigationEnd => e instanceof NavigationEnd)
    ).subscribe((e: NavigationEnd) => {
      this.updateBreadcrumb(e.urlAfterRedirects || e.url);
    });
  }

  ngOnDestroy(): void {
    if (this.routerSub) {
      this.routerSub.unsubscribe();
    }
  }

  private updateBreadcrumb(url: string): void {
    const cleanUrl = url.split('?')[0].split('#')[0];

    for (const group of NAVIGATION_MENU) {
      if (group.items) {
        for (const item of group.items) {
          if (item.routerLink && (cleanUrl === item.routerLink || cleanUrl.startsWith(item.routerLink + '/'))) {
            const rawParent = group.label || 'Master';
            const parentFormatted = rawParent.charAt(0).toUpperCase() + rawParent.slice(1).toLowerCase();
            this.breadcrumb = {
              parent: parentFormatted,
              child: item.label
            };
            return;
          }
        }
      }
    }

    const segments = cleanUrl.split('/').filter(s => s.length > 0);
    if (segments.length >= 2) {
      this.breadcrumb = {
        parent: segments[0].charAt(0).toUpperCase() + segments[0].slice(1).toLowerCase(),
        child: segments[1].charAt(0).toUpperCase() + segments[1].slice(1).toLowerCase()
      };
    } else if (segments.length === 1) {
      this.breadcrumb = {
        parent: 'Home',
        child: segments[0].charAt(0).toUpperCase() + segments[0].slice(1).toLowerCase()
      };
    } else {
      this.breadcrumb = { parent: 'Master', child: 'Inventory' };
    }
  }

  get displayName(): string {
    return this.authService.getUserName();
  }

  get userInitial(): string {
    return this.authService.getUserInitial();
  }

  get userRole(): string {
    const roles = this.authService.getUserRoles();
    return roles.length > 0 ? roles[0] : 'User';
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.router.navigate(['/auth/login']),
      error: () => this.router.navigate(['/auth/login'])
    });
  }
}
