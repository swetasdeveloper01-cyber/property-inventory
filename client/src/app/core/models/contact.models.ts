import { PaginationQuery } from './paged-result';

/** Matches backend ContactDto. */
export interface ContactDto {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  email: string;
}

export interface CreateContactRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  email: string;
}

export interface UpdateContactRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  email: string;
}

export interface UpdateContactBatchItem extends UpdateContactRequest {
  id: string;
}

export interface ContactQuery extends PaginationQuery {
  firstName?: string;
  lastName?: string;
  email?: string;
  phone?: string;
}
