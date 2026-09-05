/** Matches backend SalesDashboardItemDto. */
export interface SalesDashboardItemDto {
  id: string;
  propertyName: string;
  askingPrice: number;
  askingCurrency: string;
  owner: string;
  dateOfPurchase: string;
  soldAtPrice: number;
  soldAtCurrency: string;
  soldAtPriceUsd: number;
}
