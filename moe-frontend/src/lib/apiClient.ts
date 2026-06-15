const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5001';

// ── Error type ────────────────────────────────────────────────────────────────

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly correlationId?: string,
    public readonly details?: unknown
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

// ── Core fetch wrapper ────────────────────────────────────────────────────────

export async function apiFetch<T>(
  path: string,
  token?: string,
  options?: RequestInit
): Promise<T> {
  const url = `${API_BASE}${path}`;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options?.headers as Record<string, string>),
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    ...options,
    credentials: 'include',
    headers,
  });

  const correlationId = response.headers.get('X-Correlation-ID') ?? undefined;

  if (!response.ok) {
    let problem: { title?: string; detail?: string } = {};
    try {
      problem = await response.json();
    } catch {
      // ignore parse error
    }
    throw new ApiError(
      response.status,
      problem.title ?? `Request failed with status ${response.status}`,
      correlationId,
      problem
    );
  }

  if (response.status === 204) return undefined as T;

  return response.json() as Promise<T>;
}

// ── Type definitions ──────────────────────────────────────────────────────────

export interface CitizenRecord {
  id: string;
  nric: string;
  fullName: string;
  dateOfBirth: string;
  educationAccount?: {
    status: 'NotYetCreated' | 'Active' | 'Suspended' | 'Closed';
    balance: number;
    openedDate?: string;
    closedDate?: string;
    closureReason?: string;
  };
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  totalAmount: number;
  educationAccountPortion: number;
  externalPspPortion: number;
  status: 'Pending' | 'PartiallyPaid' | 'Paid' | 'Overdue' | 'Cancelled' | 'Refunded';
  issuedAt: string;
  paidAt?: string;
  lineItems?: { id: string; description: string; amount: number }[];
}

export interface PaymentIntentResult {
  invoiceId: string;
  totalAmount: number;
  eaPortion: number;
  pspPortion: number;
  checkoutUrl?: string;
  requiresPspPayment: boolean;
}

export interface AiChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

export interface AiChatResponse {
  reply: string;
  isGrounded: boolean;
  citationSource?: string;
  updatedDraftFields?: Record<string, unknown>;
}

export interface FasDraftFields {
  household_income?: number;
  household_size?: number;
  num_dependants?: number;
  school_name?: string;
  year_of_study?: string;
  reason_for_application?: string;
}

export interface EducationAccountTransaction {
  id: string;
  amount: number;
  transactionType: string;
  description: string;
  transactionDate: string;
}

// ── Typed API surface ─────────────────────────────────────────────────────────

export const api = {
  eligibility: {
    /** Returns the authenticated citizen's own Education Account record */
    me: (token: string) =>
      apiFetch<CitizenRecord>('/api/eligibility/me', token),

    /** Returns the authenticated citizen's ledger transactions */
    myTransactions: (token: string) =>
      apiFetch<EducationAccountTransaction[]>('/api/eligibility/me/transactions', token),

    /** Admin: triggers lifecycle evaluation for a specific citizen */
    evaluate: (citizenId: string, token: string) =>
      apiFetch<CitizenRecord>(`/api/eligibility/${citizenId}/evaluate`, token, { method: 'POST' }),
  },

  payments: {
    /** Returns all invoices for the authenticated citizen */
    myInvoices: (token: string) =>
      apiFetch<Invoice[]>('/api/payments/invoices', token),

    /** Returns a single invoice by ID */
    getInvoice: (id: string, token: string) =>
      apiFetch<Invoice>(`/api/payments/invoices/${id}`, token),

    /** Actively verifies a payment status with the backend */
    verifyPayment: (id: string, token: string) =>
      apiFetch<Invoice>(`/api/payments/verify/${id}`, token, { method: 'POST' }),

    /** Creates a payment intent. Returns checkout URL if PSP payment needed. */
    createIntent: (invoiceId: string, payerEmail: string, token: string) =>
      apiFetch<PaymentIntentResult>('/api/payments/intents', token, {
        method: 'POST',
        body: JSON.stringify({ invoiceId, payerEmail }),
      }),
  },

  ai: {
    /** Sends a chat message. Supports 'support' and 'fas-draft' modes. */
    chat: (
      message: string,
      mode: 'support' | 'fas-draft',
      history: AiChatMessage[],
      token: string,
      citizenContextJson?: string
    ) =>
      apiFetch<AiChatResponse>('/api/chat', token, {
        method: 'POST',
        body: JSON.stringify({
          userMessage: message,
          mode,
          history,
          citizenContextJson: citizenContextJson ?? null,
        }),
      }),
  },

  admin: {
    /** Returns the authenticated admin's identity and roles (Entra ID) */
    me: (token: string) =>
      apiFetch<{ userId: string; name: string; email: string; roles: string[] }>(
        '/api/auth/admin/me',
        token
      ),

    accounts: {
      list: (token: string) => 
        apiFetch<(CitizenRecord & { age: number })[]>('/api/admin/accounts', token),
      
      get: (id: string, token: string) =>
        apiFetch<{ record: CitizenRecord; transactions: EducationAccountTransaction[] }>(`/api/admin/accounts/${id}`, token),
      
      create: (id: string, reason: string, token: string) =>
        apiFetch<CitizenRecord>(`/api/admin/accounts/${id}/create`, token, {
          method: 'POST',
          body: JSON.stringify({ reason })
        }),
      
      overrideStatus: (id: string, status: string, reason: string, token: string) =>
        apiFetch<CitizenRecord>(`/api/admin/accounts/${id}/override`, token, {
          method: 'POST',
          body: JSON.stringify({ status, reason })
        }),
        
      topUp: (id: string, amount: number, reason: string, token: string) =>
        apiFetch<CitizenRecord>(`/api/admin/accounts/${id}/topup`, token, {
          method: 'POST',
          body: JSON.stringify({ amount, reason })
        }),
    },

    billing: {
      getCourses: (token: string) =>
        apiFetch<any[]>('/api/admin/billing/courses', token),
        
      createCourse: (name: string, description: string, feeComponents: { name: string, amount: number, isGstApplicable: boolean }[], token: string) =>
        apiFetch<any>('/api/admin/billing/courses', token, {
          method: 'POST',
          body: JSON.stringify({ name, description, feeComponents })
        }),

      enroll: (courseId: string, citizenId: string, token: string) =>
        apiFetch<any>(`/api/admin/billing/courses/${courseId}/enroll`, token, {
          method: 'POST',
          body: JSON.stringify({ citizenId })
        }),
    }
  },

  fas: {
    list: (token: string) =>
      apiFetch<any[]>('/api/fas/admin/list', token),
    get: (id: string, token: string) =>
      apiFetch<any>(`/api/fas/admin/${id}`, token),
    review: (id: string, status: string, remarks: string, token: string) =>
      apiFetch<any>(`/api/fas/admin/${id}/review`, token, {
        method: 'POST',
        body: JSON.stringify({ status, remarks })
      }),
    approve: (id: string, status: string, remarks: string, token: string) =>
      apiFetch<any>(`/api/fas/admin/${id}/approve`, token, {
        method: 'POST',
        body: JSON.stringify({ status, remarks })
      }),
    submit: (applicationDataJson: string, token: string) =>
      apiFetch<any>('/api/fas/submit', token, {
        method: 'POST',
        body: JSON.stringify({ applicationDataJson })
      })
  }
};
