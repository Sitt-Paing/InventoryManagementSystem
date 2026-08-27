import { Component, OnInit, OnDestroy, HostListener, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, NavigationEnd, Event } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { SharedService } from '../../core/services/shared.service';
import { AuthService } from '../../core/services/auth.service';
import { TranslationService } from '../../core/services/translation.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { LanguageCode } from '../../core/i18n/languages';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { NAVIGATION_MENU, NavigationMenuGroup, NavigationMenuItem } from '../../app.menu';

export interface BreadcrumbInfo {
  parent: string;
  parentTransKey?: string;
  child: string;
  childTransKey?: string;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    MenuModule,
    TranslatePipe
  ],
  templateUrl: './app-header.html',
  styleUrl: './app-header.scss'
})
export class AppHeader implements OnInit, OnDestroy {
  breadcrumb: BreadcrumbInfo = {
    parent: 'Master',
    parentTransKey: 'NAV.MASTER',
    child: 'Categories',
    childTransKey: 'NAV.CATEGORIES'
  };
  private routerSub!: Subscription;
  private elementRef = inject(ElementRef);

  isLangDropdownOpen = false;

  constructor(
    public sharedService: SharedService,
    public translationService: TranslationService,
    private authService: AuthService,
    private router: Router
  ) {}

  get userMenuItems(): MenuItem[] {
    return [
      {
        label: this.translationService.translate('HEADER.MY_PROFILE'),
        icon: 'pi pi-user'
      },
      {
        label: this.translationService.translate('HEADER.SYSTEM_SETTINGS'),
        icon: 'pi pi-cog'
      },
      { separator: true },
      {
        label: this.translationService.translate('HEADER.LOGOUT'),
        icon: 'pi pi-power-off',
        styleClass: 'text-red-500',
        command: () => this.logout()
      }
    ];
  }

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

  toggleLangDropdown(event: MouseEvent): void {
    event.stopPropagation();
    this.isLangDropdownOpen = !this.isLangDropdownOpen;
  }

  selectLanguage(lang: LanguageCode): void {
    this.translationService.setLanguage(lang);
    this.isLangDropdownOpen = false;
    this.updateBreadcrumb(this.router.url);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.isLangDropdownOpen && !this.elementRef.nativeElement.contains(event.target)) {
      this.isLangDropdownOpen = false;
    }
  }

  private updateBreadcrumb(url: string): void {
    const cleanUrl = url.split('?')[0].split('#')[0];

    for (const group of NAVIGATION_MENU) {
      if (group.items) {
        for (const item of group.items) {
          if (item.routerLink && (cleanUrl === item.routerLink || cleanUrl.startsWith(item.routerLink + '/'))) {
            this.breadcrumb = {
              parent: group.label,
              parentTransKey: group.transKey,
              child: item.label,
              childTransKey: item.transKey
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
        parentTransKey: 'NAV.HOME',
        child: segments[0].charAt(0).toUpperCase() + segments[0].slice(1).toLowerCase()
      };
    } else {
      this.breadcrumb = {
        parent: 'Master',
        parentTransKey: 'NAV.MASTER',
        child: 'Categories',
        childTransKey: 'NAV.CATEGORIES'
      };
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
