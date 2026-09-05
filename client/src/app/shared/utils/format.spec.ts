import { formatBusinessDate, formatMoney, formatShortId } from './format';

describe('format utilities', () => {
  it('formats business dates without timezone shift', () => {
    expect(formatBusinessDate('2024-01-15')).toBe('15 Jan 2024');
    expect(formatBusinessDate('2023-07-25')).toBe('25 Jul 2023');
  });

  it('formats money using the provided currency', () => {
    expect(formatMoney(130000, 'EUR')).toContain('130,000.00');
    expect(formatMoney(130480, 'USD')).toContain('130,480.00');
    expect(formatMoney(1000, 'GBP')).toContain('1,000.00');
  });

  it('shortens GUIDs for table display', () => {
    expect(formatShortId('e2222222-2222-2222-2222-222222222222')).toBe('e2222222…');
  });
});
