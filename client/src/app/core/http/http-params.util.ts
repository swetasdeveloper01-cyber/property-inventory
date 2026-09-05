import { HttpParams } from '@angular/common/http';

/** Builds HttpParams from a plain object, omitting null/undefined/empty values. */
export function toHttpParams(query: Record<string, string | number | boolean | null | undefined>): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(query)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }

    params = params.set(key, String(value));
  }

  return params;
}
