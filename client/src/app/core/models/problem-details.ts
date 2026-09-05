/** Subset of ASP.NET Core ProblemDetails used by the API. */
export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ApiProblemDetails;

  constructor(status: number, problem: ApiProblemDetails) {
    super(problem.detail ?? problem.title ?? `HTTP ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }

  get fieldErrors(): Record<string, string[]> {
    return this.problem.errors ?? {};
  }
}
