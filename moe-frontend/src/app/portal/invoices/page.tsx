"use client";

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { api, Invoice } from '@/lib/apiClient';

export default function InvoicesPage() {
  const router = useRouter();
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1];
    if (!token) {
      router.push('/login');
      return;
    }

    api.payments.myInvoices(token)
      .then(data => {
        setInvoices(data);
      })
      .catch(err => {
        setError(err.message || "Failed to load invoices");
      })
      .finally(() => {
        setLoading(false);
      });
  }, [router]);

  if (loading) return <div className="flex-center" style={{ minHeight: '40vh' }}>Loading...</div>;

  if (error) {
    return (
      <div className="glass-panel" style={{ textAlign: 'center' }}>
        <h2 style={{ color: 'var(--accent-color)' }}>Error</h2>
        <p style={{ color: 'var(--text-secondary)' }}>{error}</p>
      </div>
    );
  }

  const pendingInvoices = invoices.filter(i => i.status === 'Pending' || i.status === 'PartiallyPaid');

  return (
    <div className="glass-panel">
      <h2 style={{ marginTop: 0 }}>Pending Invoices</h2>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '24px' }}>
        {pendingInvoices.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)' }}>You have no pending invoices.</p>
        ) : (
          pendingInvoices.map(inv => (
            <div key={inv.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '16px', border: '1px solid var(--glass-border)', borderRadius: '8px', background: 'rgba(0,0,0,0.2)' }}>
              <div>
                <div style={{ fontWeight: 'bold' }}>{inv.invoiceNumber}</div>
                <div style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Amount: S$ {inv.totalAmount.toFixed(2)}</div>
              </div>
              <Link href={`/portal/payments?invoiceId=${inv.id}`} className="btn-primary" style={{ alignSelf: 'center', textDecoration: 'none' }}>Pay Now</Link>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
