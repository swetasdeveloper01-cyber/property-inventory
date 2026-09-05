/**
 * Formats an ASP.NET DateOnly ISO string (yyyy-MM-dd) without timezone shifting.
 */
export function formatBusinessDate(isoDate: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate.trim());
  if (!match) {
    return isoDate;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

  if (month < 1 || month > 12 || day < 1 || day > 31) {
    return isoDate;
  }

  return `${day} ${months[month - 1]} ${year}`;
}

/**
 * Formats a monetary amount with the supplied ISO currency code.
 * Does not convert currencies.
 */
export function formatMoney(amount: number, currency: string): string {
  const code = currency?.trim().toUpperCase() || 'EUR';

  try {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: code,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  } catch {
    return `${code} ${amount.toFixed(2)}`;
  }
}

/** Short GUID display; full value should remain available via title/tooltip. */
export function formatShortId(id: string): string {
  if (!id) {
    return '';
  }

  return id.length <= 8 ? id : `${id.slice(0, 8)}…`;
}
