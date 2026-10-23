import { LanguageCode } from '../../i18n/languages';

export interface DashboardProductStock {
  productId: string;
  productName: string;
  baseUnit: string;
  baseUnitSymbol: string;
  availableQuantity: number;
  reorderLevel: number;
}

export interface DashboardDailyUsage {
  date: string;
  productId: string;
  productName: string;
  baseUnit: string;
  baseUnitSymbol: string;
  issuedQuantity: number;
}

export interface DashboardFacts {
  dataAsOfUtc: string;
  snapshotDate: string;
  startDate: string;
  endDate: string;
  activeProductCount: number;
  openPurchaseOrderCount: number;
  productStocks: DashboardProductStock[];
  dailyProductUsage: DashboardDailyUsage[];
  stockHealth: { aboveReorder: number; lowStock: number; outOfStock: number };
}

export type DashboardWidgetType =
  | 'metric_card'
  | 'table'
  | 'bar_chart'
  | 'line_chart'
  | 'area_chart'
  | 'stacked_bar_chart'
  | 'pie_chart'
  | 'doughnut_chart';

export type DashboardDatasetKey =
  | 'activeProductCount'
  | 'openPurchaseOrderCount'
  | 'productStocks'
  | 'dailyProductUsage';

export interface DashboardFieldLabel {
  field: string;
  text: string;
}

export interface DashboardWidget {
  id: string;
  type: DashboardWidgetType;
  datasetKey: DashboardDatasetKey;
  title: string;
  description: string;
  productIds: string[];
  labels: DashboardFieldLabel[];
}

export interface DashboardDefinition {
  language: LanguageCode;
  title: string;
  summary: string;
  widgets: DashboardWidget[];
}

export interface DashboardGeneration {
  facts: DashboardFacts;
  definition: DashboardDefinition;
}

export interface DashboardGenerateRequest {
  startDate: string;
  endDate: string;
  language: LanguageCode;
}

export interface DashboardApiResponse<T> {
  statusCode: number;
  success: boolean;
  message: string | null;
  data: T | null;
}
