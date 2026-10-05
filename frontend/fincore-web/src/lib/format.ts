export const CURRENCIES = ['TRY', 'USD', 'EUR']

export const CATEGORIES = [
    'General',
    'Shopping',
    'Food',
    'Bills',
    'Rent',
    'Transport',
    'Entertainment',
    'Health',
    'Education',
    'Other',
]

// The API sends UTC dates; some arrive without a "Z" suffix. Treat them as UTC.
export function parseUtc(value: string): Date {
    const hasTimezone = /([zZ]|[+-]\d{2}:\d{2})$/.test(value)
    return new Date(hasTimezone ? value : `${value}Z`)
}

export function formatMoney(amount: number, currency = 'TRY'): string {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency,
        currencyDisplay: 'narrowSymbol',
    }).format(amount)
}

export function formatCompact(amount: number): string {
    return new Intl.NumberFormat('en-US', { notation: 'compact', maximumFractionDigits: 1 }).format(amount)
}

export function formatDateTime(value: string): string {
    return new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short' }).format(parseUtc(value))
}

export function formatDate(value: string): string {
    return new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium' }).format(parseUtc(value))
}

// "2026-10" -> "Oct"
export function formatMonthLabel(month: string): string {
    const [year, monthNumber] = month.split('-').map(Number)
    return new Intl.DateTimeFormat('en-US', { month: 'short', timeZone: 'UTC' }).format(
        new Date(Date.UTC(year, monthNumber - 1, 1)),
    )
}