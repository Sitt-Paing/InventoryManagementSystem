import { LanguageCode } from '../i18n/languages';

export interface AiDashboardProduct {
  id: string;
  name: string;
  unit: string;
  stock: number;
  dailyUsage: number | null;
  leadDays: number;
  coverDays: number | null;
  risk: 'RISK' | 'SUFFICIENT' | 'UNKNOWN';
}

export interface AiDashboardProductCopy {
  id: string;
  displayName: string;
  unitLabel: string;
  statusLabel: string;
  explanation: string;
}

export interface AiDashboardData {
  language: LanguageCode;
  isSampleData: boolean;
  facts: {
    snapshotDate: string;
    products: AiDashboardProduct[];
    orders: { number: string; supplier: string; expected: string; overdue: boolean }[];
  };
  content: {
    title: string;
    summary: string;
    labels: Record<string, string>;
    products: AiDashboardProductCopy[];
  };
}

export interface AiDashboardResponse {
  success: boolean;
  message: string | null;
  data: AiDashboardData | null;
}
