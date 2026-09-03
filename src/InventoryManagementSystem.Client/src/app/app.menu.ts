const ADMIN: string[] = ['admin', 'administrator'];
const ADMIN_COMPANY: string[] = ['admin', 'administrator', 'company'];
const ADMIN_COMPANY_MANAGER: string[] = ['admin', 'administrator', 'company', 'manager'];
const SELF_SERVICE: string[] = ['admin', 'administrator', 'company', 'manager', 'employee'];
const MANAGER_PROCESS: string[] = ['admin', 'administrator', 'company', 'manager'];
const LEAVE_REQUEST_ROLES: string[] = ['admin', 'administrator', 'company', 'manager', 'departmenthead', 'employee'];
const REPORT_ROLES: string[] = ['admin', 'administrator', 'company', 'manager', 'departmenthead'];
const ALL_ROLES: string[] = ['admin', 'administrator', 'company', 'manager', 'departmenthead', 'employee'];

export function hasAnyMenuRole(
  item: any,
  userRoles: string | string[] | null | undefined,
): boolean {
  if (!item?.data?.roles && !item?.data?.role) {
    return true;
  }

  const allowedRoles: string[] = (item?.data?.roles ?? item?.data?.role ?? ALL_ROLES).map((role: string) =>
    role.toLowerCase(),
  );

  const currentRoles: string[] = (Array.isArray(userRoles) ? userRoles : (userRoles ?? '').split(','))
    .map((role) => role.trim().toLowerCase())
    .filter((role) => role.length > 0);

  if (currentRoles.length === 0 || currentRoles.some(r => r.includes('admin'))) {
    return true;
  }

  return currentRoles.some((role) =>
    allowedRoles.some(allowed => allowed.includes(role) || role.includes(allowed))
  );
}

export interface NavigationMenuItem {
  label: string;
  transKey?: string;
  icon: string;
  routerLink?: string;
  data?: any;
  items?: NavigationMenuItem[];
}

export interface NavigationMenuGroup {
  label: string;
  transKey?: string;
  items: NavigationMenuItem[];
}

export const NAVIGATION_MENU: Readonly<NavigationMenuGroup[]> = [
  {
    label: 'HOME',
    transKey: 'NAV.HOME',
    items: [
      {
        label: 'Dashboard',
        transKey: 'NAV.DASHBOARD',
        icon: 'pi pi-home',
        routerLink: '/dashboard',
        data: { roles: ALL_ROLES },
      },
    ],
  },
  {
    label: 'MASTER',
    transKey: 'NAV.MASTER',
    items: [
      {
        label: 'Categories',
        transKey: 'NAV.CATEGORIES',
        icon: 'pi pi-tags',
        routerLink: '/master/categories',
      },
      {
        label: 'Products',
        transKey: 'NAV.PRODUCTS',
        icon: 'pi pi-box',
        routerLink: '/master/products',
      },
    ],
  },
  // {
  //   label: 'PROCESS',
  //   transKey: 'NAV.PROCESS',
  //   items: [
  //     {
  //       label: 'Stock Transactions',
  //       transKey: 'NAV.STOCK_TRANSACTIONS',
  //       icon: 'pi pi-arrow-right-left',
  //       routerLink: '/process/stock-transactions',
  //       data: { roles: MANAGER_PROCESS },
  //     },
  //   ],
  // },
];

