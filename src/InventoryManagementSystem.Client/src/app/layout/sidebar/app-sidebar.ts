import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SharedService } from '../../core/services/shared.service';

export interface NavItem {
  label: string;
  icon: string;
  routerLink?: string;
  badge?: string;
  badgeSeverity?: string;
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
  navGroups: NavGroup[] = [
    {
      groupName: 'OVERVIEW',
      items: [
        { label: 'Dashboard', icon: 'pi pi-home', routerLink: '/dashboard' }
      ]
    },
    {
      groupName: 'INVENTORY & CATALOG',
      items: [
        { label: 'Products Master', icon: 'pi pi-box', routerLink: '/products', badge: '142', badgeSeverity: 'bg-indigo-500/20 text-indigo-300' },
        { label: 'Categories', icon: 'pi pi-tags', routerLink: '/categories' },
        { label: 'Stock Transactions', icon: 'pi pi-arrow-right-left', routerLink: '/stock-transactions', badge: 'NEW', badgeSeverity: 'bg-emerald-500/20 text-emerald-300' }
      ]
    },
    {
      groupName: 'REPORTS & ALERTS',
      items: [
        { label: 'Low Stock Alerts', icon: 'pi pi-exclamation-triangle', routerLink: '/dashboard', badge: '14', badgeSeverity: 'bg-amber-500/20 text-amber-300' },
        { label: 'Inventory Valuation', icon: 'pi pi-chart-bar', routerLink: '/products' }
      ]
    },
    {
      groupName: 'ADMINISTRATION',
      items: [
        { label: 'User Management', icon: 'pi pi-users', routerLink: '/dashboard' },
        { label: 'Company & Branches', icon: 'pi pi-building', routerLink: '/dashboard' },
        { label: 'Settings', icon: 'pi pi-cog', routerLink: '/dashboard' }
      ]
    }
  ];

  constructor(public sharedService: SharedService) {}
}
