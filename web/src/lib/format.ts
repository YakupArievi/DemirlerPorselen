export const tl = (v: number) =>
  new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(v ?? 0);

export const dateStr = (s: string) =>
  new Date(s).toLocaleDateString('tr-TR');

export const dateTimeStr = (s: string) =>
  new Date(s).toLocaleString('tr-TR');
