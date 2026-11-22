import { HttpErrorResponse } from '@angular/common/http';

// Shape of RFC 9457 problem details returned by the API.
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

const FALLBACK_MESSAGES: Record<number, string> = {
  0: 'לא ניתן להתחבר לשרת. ודא/י שהשרת פועל.',
  401: 'ההרשאה פגה. נסה/י שוב.',
  404: 'הסשן לא נמצא או שפג תוקפו. יש ללחוץ על "איפוס" ולהתחיל מחדש.',
  429: 'יותר מדי בקשות. המתן/י רגע ונסה/י שוב.',
};

// Turns any HTTP failure into a single user-facing message.
export function toUserMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'אירעה שגיאה בלתי צפויה.';
  }

  if (FALLBACK_MESSAGES[error.status]) {
    return FALLBACK_MESSAGES[error.status];
  }

  const problem = error.error as ProblemDetails | null;
  const validationErrors = problem?.errors ? Object.values(problem.errors).flat() : [];
  if (validationErrors.length > 0) {
    return validationErrors.join(' ');
  }

  return problem?.detail ?? problem?.title ?? 'אירעה שגיאה בלתי צפויה.';
}
