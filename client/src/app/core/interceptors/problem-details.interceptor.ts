import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError, ApiProblemDetails } from '../models/problem-details';

/**
 * Maps ASP.NET Core ProblemDetails responses into a typed ApiError for UI consumption.
 */
export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const problem = toProblemDetails(error);
      return throwError(() => new ApiError(error.status, problem));
    })
  );

function toProblemDetails(error: HttpErrorResponse): ApiProblemDetails {
  const body = error.error;

  if (body && typeof body === 'object') {
    const problem = body as ApiProblemDetails;
    return {
      type: problem.type,
      title: problem.title ?? error.statusText,
      status: problem.status ?? error.status,
      detail: problem.detail,
      instance: problem.instance,
      traceId: problem.traceId,
      errors: problem.errors
    };
  }

  return {
    title: error.statusText || 'Request failed',
    status: error.status,
    detail: typeof body === 'string' && body.length > 0 ? body : error.message
  };
}
