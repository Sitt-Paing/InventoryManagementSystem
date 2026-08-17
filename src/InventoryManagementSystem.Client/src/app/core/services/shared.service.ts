import { Injectable, signal } from '@angular/core';
import { CompanyModel, BranchModel } from '../models/company-branch.model';

@Injectable({
  providedIn: 'root'
})
export class SharedService {

  sidebarCollapsed = signal<boolean>(false);
  isDarkMode = signal<boolean>(this.loadDarkModePreference());

  constructor() {
    // Apply initial dark mode class on startup
    this.applyDarkModeClass(this.isDarkMode());
  }

  private loadDarkModePreference(): boolean {
    const saved = localStorage.getItem('theme');
    if (saved !== null) {
      return saved === 'dark';
    }
    // Default to light mode
    return false;
  }

  private applyDarkModeClass(dark: boolean): void {
    if (dark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }

  // getUserName(): string {
  //   const raw = localStorage.getItem('AUTH_USER');
  //   if (raw) {
  //     try {
  //       return JSON.parse(raw)?.userName ?? 'Guest';
  //     } catch { return 'Guest'; }
  //   }
  //   return 'Guest';
  // }

  getUserName(): string {
    return localStorage.getItem('userName') ?? 'Guest';
  }

  getUserInitial(): string {
    const name = this.getUserName();
    return (name && name.length > 0 ? name.charAt(0) : 'U').toUpperCase();
  }

  getDefaultCompany(): string | null {
    return localStorage.getItem('DEFAULT_COMPANY');
  }

  getDefaultBranch(): string | null {
    return localStorage.getItem('DEFAULT_BRANCH');
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(val => !val);
  }

  toggleDarkMode(): void {
    this.isDarkMode.update(val => {
      const next = !val;
      localStorage.setItem('theme', next ? 'dark' : 'light');
      this.applyDarkModeClass(next);
      return next;
    });
  }
}
