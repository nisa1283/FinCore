// Backend'in her cevapta kullandığı standart zarf
export interface ApiResponse<T> {
    success: boolean
    message?: string | null
    data: T
    errors?: string[] | null
}

export interface PagedResult<T> {
    items: T[]
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
}

export interface AuthResponse {
    accessToken: string
    refreshToken: string
    accessTokenExpiresAt: string
}

export interface UserProfile {
    id: string
    email: string
    fullName: string
    role: 'Customer' | 'Admin'
}
export interface Account {
  id: string
  accountNumber: string
  name: string
  currency: string
  balance: number
  status: 'Active' | 'Frozen'
  createdAt: string
}

export interface CreateAccountRequest {
  name: string
  currency: string
}

export type TransactionStatus = 'Pending' | 'Completed' | 'Failed'

export interface TransferRequest {
  sourceAccountId: string
  targetAccountNumber: string
  amount: number
  description?: string
  category?: string
}

export interface TransferResult {
  id: string
  status: TransactionStatus
  amount: number
  currency: string | null
  sourceAccountNumber: string | null
  targetAccountNumber: string
  description: string | null
  category: string
  failureReason: string | null
  createdAt: string
  isDuplicate: boolean
}

export interface TransactionItem {
  id: string
  date: string
  description: string | null
  category: string
  amount: number
  currency: string | null
  status: TransactionStatus
  type: 'Incoming' | 'Outgoing'
  counterpartyAccountNumber: string | null
  failureReason: string | null
}

export interface TransactionFilters {
  page: number
  pageSize: number
  search?: string
  status?: string
  type?: string
  category?: string
  from?: string
  to?: string
  minAmount?: string
  maxAmount?: string
  accountId?: string
}

export interface MonthlyFlow {
  month: string
  income: number
  expense: number
}

export interface CategorySpend {
  category: string
  total: number
}

export interface TransactionSummary {
  currency: string
  totalIncome: number
  totalExpense: number
  months: MonthlyFlow[]
  categories: CategorySpend[]
}

export interface AppNotification {
  id: string
  type: string
  title: string
  message: string
  referenceId: string | null
  isRead: boolean
  createdAt: string
}