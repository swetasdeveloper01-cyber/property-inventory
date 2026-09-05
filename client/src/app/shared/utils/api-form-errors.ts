/**
 * Maps ASP.NET ProblemDetails field keys (PascalCase) onto Angular form control names.
 */
export function applyApiFieldErrors(
  errors: Record<string, string[]> | undefined,
  setError: (controlName: string, message: string) => void
): void {
  if (!errors) {
    return;
  }

  for (const [key, messages] of Object.entries(errors)) {
    if (!messages?.length) {
      continue;
    }

    const controlName = key.includes('.')
      ? key.split('.').pop()!.replace(/^./, (c) => c.toLowerCase())
      : key.charAt(0).toLowerCase() + key.slice(1);

    setError(controlName, messages[0]);
  }
}
