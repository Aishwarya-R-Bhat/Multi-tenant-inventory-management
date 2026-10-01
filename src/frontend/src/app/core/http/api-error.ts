import { HttpErrorResponse } from '@angular/common/http';

/** A ProblemDetails response from the API, reduced to what the UI needs. */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
  }
}

export function toApiError(err: HttpErrorResponse): ApiError {
  if (err.status === 0) return new ApiError('Cannot reach the server. Check your connection and try again.', 0);

  const problem = err.error as { title?: string; errors?: Record<string, string[]> } | null;
  const message =
    err.status >= 500 ? 'Something went wrong on our side. Please try again.' : (problem?.title ?? err.message);
  return new ApiError(message, err.status, problem?.errors ?? {});
}
