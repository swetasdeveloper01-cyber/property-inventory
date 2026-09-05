import { PaginationQuery } from './paged-result';

/** Matches backend PropertyDto. Dates are ISO date strings (DateOnly). */
export interface PropertyDto {
  id: string;
  name: string;
  address: string;
  price: number;
  currency: string;
  dateOfRegistration: string;
}

export interface CreatePropertyRequest {
  name: string;
  address: string;
  price: number;
  currency: string;
  dateOfRegistration: string;
}

export interface UpdatePropertyRequest {
  name: string;
  address: string;
  price: number;
  currency: string;
  dateOfRegistration: string;
}

export interface UpdatePropertyBatchItem extends UpdatePropertyRequest {
  id: string;
}

export interface PropertyQuery extends PaginationQuery {
  name?: string;
  address?: string;
  minPrice?: number;
  maxPrice?: number;
}
