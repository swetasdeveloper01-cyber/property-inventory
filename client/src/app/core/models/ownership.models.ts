/** Matches backend OwnershipDto. Dates are ISO date strings (DateOnly). */
export interface OwnershipDto {
  id: string;
  propertyId: string;
  contactId: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerEmail: string;
  effectiveFrom: string;
  effectiveTill: string | null;
  acquisitionPrice: number;
  acquisitionCurrency: string;
  acquisitionPriceUsd: number;
  isCurrent: boolean;
}

export interface CreateOwnershipRequest {
  contactId: string;
  effectiveFrom: string;
  effectiveTill?: string | null;
  acquisitionPrice: number;
  acquisitionCurrency: string;
}
