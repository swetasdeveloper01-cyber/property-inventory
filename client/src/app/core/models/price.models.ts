/** Matches backend PropertyPriceDto. */
export interface PropertyPriceDto {
  id: string;
  propertyId: string;
  amount: number;
  currency: string;
  effectiveDate: string;
}

export interface CreatePropertyPriceRequest {
  amount: number;
  currency: string;
  effectiveDate: string;
}
