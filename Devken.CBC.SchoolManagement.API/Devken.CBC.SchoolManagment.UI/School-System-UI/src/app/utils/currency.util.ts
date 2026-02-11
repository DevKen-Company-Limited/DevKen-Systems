// export function formatKES(amount?: number | null): string {
//   if (amount === null || amount === undefined || isNaN(amount)) {
//     return 'KSh 0';
//   }

//   return `KSh ${Number(amount).toLocaleString('en-KE', {
//     minimumFractionDigits: 0,
//     maximumFractionDigits: 2
//   })}`;
// }


export function formatKES(amount?: number | null): string {
  if (amount === null || amount === undefined || isNaN(amount)) {
    return 'KSh 0';
  }

  return `KSh ${Number(amount).toLocaleString('en-KE')}`;
}
