export type UserRole = 'Admin' | 'Patron' | 'Depocu';
export type UnitType = 'Adet' | 'Koli';
export type PaymentType = 'Nakit' | 'Kart' | 'Cek';
export type SaleStatus = 'Active' | 'Cancelled';

export interface UserInfo {
  id: string;
  userName: string;
  fullName: string;
  role: UserRole;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: UserInfo;
}

export interface Paged<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface Category { id: string; name: string; description?: string; isActive: boolean; }
export interface Brand { id: string; name: string; isActive: boolean; }

export interface Product {
  id: string; name: string; description?: string;
  categoryId: string; categoryName: string;
  brandId?: string; brandName?: string;
  isActive: boolean; variantCount: number;
}

export interface Variant {
  id: string; productId: string; productName: string;
  color?: string; pattern?: string; size?: string;
  adetBarcode: string; koliBarcode: string; koliIciAdet: number;
  purchasePrice: number; salePrice: number; averageCost: number;
  minStock: number; imageUrl?: string; isActive: boolean;
}

export interface ResolvedBarcode {
  variantId: string; productName: string;
  color?: string; pattern?: string; size?: string;
  unitType: UnitType; adetEquivalent: number; salePrice: number;
}

export interface Warehouse { id: string; name: string; code?: string; isDefault: boolean; isActive: boolean; }

export interface StockLevel {
  variantId: string; productName: string; color?: string; pattern?: string; size?: string;
  adetBarcode: string; warehouseId: string; warehouseName: string;
  quantity: number; minStock: number; isBelowMin: boolean;
}

export interface Customer {
  id: string; name: string; phone?: string; address?: string; taxNumber?: string; notes?: string;
  openingBalance: number; balance: number; isActive: boolean;
}

export interface SaleLine {
  id: string; variantId: string; productName: string; color?: string; size?: string;
  unitType: UnitType; quantity: number; adetQuantity: number;
  unitPrice: number; unitCost: number; lineDiscount: number; lineTotal: number;
}

export interface Sale {
  id: string; saleNumber: number; customerId: string; customerName: string; warehouseId: string;
  saleDate: string; status: SaleStatus;
  subTotal: number; discountTotal: number; grandTotal: number; costTotal: number;
  paidAmount: number; profit: number; note?: string; cancelledAt?: string;
  lines: SaleLine[];
}

export interface SaleListItem {
  id: string; saleNumber: number; customerName: string; saleDate: string;
  status: SaleStatus; grandTotal: number; paidAmount: number;
}

export interface StatementLine {
  date: string; type: string; debit: number; credit: number; runningBalance: number;
  referenceType?: string; referenceId?: string; note?: string;
}

export interface Statement {
  customerId: string; customerName: string; fromDate: string; toDate: string;
  openingBalance: number; closingBalance: number; totalDebit: number; totalCredit: number;
  lines: StatementLine[];
}

export interface Dashboard {
  todayRevenue: number; monthRevenue: number; todayCollections: number; monthCollections: number;
  totalReceivables: number; criticalStockCount: number; brokenCountThisMonth: number; brokenQuantityThisMonth: number;
  criticalStocks: { variantId: string; productName: string; color?: string; size?: string; totalQuantity: number; minStock: number }[];
  topProducts: { variantId: string; productName: string; color?: string; size?: string; soldQuantity: number; revenue: number }[];
  recentSales: { id: string; saleNumber: number; customerName: string; saleDate: string; grandTotal: number }[];
}
