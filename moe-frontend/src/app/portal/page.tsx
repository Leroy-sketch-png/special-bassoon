"use client";

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { api, CitizenRecord, Invoice } from '@/lib/apiClient';

export default function PortalDashboard() {
  const router = useRouter();
  const [profile, setProfile] = useState<CitizenRecord | null>(null);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [transactions, setTransactions] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1];
    if (!token) {
      router.push('/login');
      return;
    }

    Promise.all([
      api.eligibility.me(token),
      api.payments.myInvoices(token),
      api.eligibility.myTransactions(token)
    ])
      .then(([profileData, invoicesData, transactionsData]) => {
        setProfile(profileData);
        setInvoices(invoicesData);
        setTransactions(transactionsData);
      })
      .catch((err) => {
        setError(err.message || "Failed to load dashboard data");
      })
      .finally(() => {
        setLoading(false);
      });
  }, [router]);

  if (loading) {
    return <div className="flex-center" style={{ minHeight: '40vh' }}>Loading...</div>;
  }

  if (error || !profile) {
    return (
      <div className="glass-panel" style={{ textAlign: 'center', marginTop: '40px' }}>
        <h2 style={{ color: 'var(--accent-color)' }}>Access Error</h2>
        <p style={{ color: 'var(--text-secondary)' }}>{error || "We couldn't fetch your profile details."}</p>
        <Link href="/login" className="btn-primary" style={{ display: 'inline-block', marginTop: '16px', textDecoration: 'none' }}>Return to Login</Link>
      </div>
    );
  }

  const pendingInvoices = invoices.filter(i => i.status === 'Pending' || i.status === 'PartiallyPaid');

  return (
    <div>
      <h1 className="page-title">Welcome back, {profile.fullName}</h1>
      
      <div className="grid-cols-2" style={{ marginTop: '32px' }}>
        <div className="glass-panel">
          <h3 style={{ marginTop: 0, color: 'var(--text-secondary)' }}>Education Account Balance</h3>
          <div style={{ fontSize: '2.5rem', fontWeight: 'bold' }}>S$ {profile.educationAccount?.balance.toFixed(2)}</div>
          <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '8px' }}>
            Status: <span style={{ color: profile.educationAccount?.status === 'Active' ? '#10b981' : '#f59e0b' }}>{profile.educationAccount?.status}</span>
          </div>
        </div>

        <div className="glass-panel">
          <h3 style={{ marginTop: 0, color: 'var(--text-secondary)' }}>Pending Invoices</h3>
          <div style={{ fontSize: '2.5rem', fontWeight: 'bold' }}>{pendingInvoices.length}</div>
        </div>
      </div>

      {pendingInvoices.length > 0 && (
        <div className="glass-panel" style={{ marginTop: '24px' }}>
          <h2 style={{ marginTop: 0 }}>Outstanding Payments</h2>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginTop: '16px' }}>
            {pendingInvoices.map(inv => (
              <div key={inv.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px', background: 'var(--bg-color)', borderRadius: '8px', border: '1px solid var(--glass-border)' }}>
                <div>
                  <div style={{ fontWeight: 600 }}>Invoice {inv.invoiceNumber}</div>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>Issued: {new Date(inv.issuedAt).toLocaleDateString()}</div>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '24px' }}>
                  <div style={{ fontSize: '1.2rem', fontWeight: 'bold' }}>S$ {inv.totalAmount.toFixed(2)}</div>
                  <Link href={`/portal/payments?invoiceId=${inv.id}`} className="btn-primary" style={{ textDecoration: 'none' }}>Pay Now</Link>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {transactions.length > 0 && (
        <div className="glass-panel" style={{ marginTop: '24px' }}>
          <h2 style={{ marginTop: 0 }}>Transaction History</h2>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginTop: '16px' }}>
            {transactions.slice(0, 5).map(t => (
              <div key={t.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px', background: 'var(--bg-color)', borderRadius: '8px', border: '1px solid var(--glass-border)' }}>
                <div>
                  <div style={{ fontWeight: 600 }}>{t.transactionType}</div>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>{new Date(t.transactionDate).toLocaleDateString()}</div>
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '4px' }}>{t.description}</div>
                </div>
                <div style={{ fontSize: '1.2rem', fontWeight: 'bold', color: t.amount > 0 ? '#10b981' : (t.amount < 0 ? '#ef4444' : 'var(--text-color)') }}>
                  {t.amount > 0 ? '+' : ''}{t.amount.toFixed(2)}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="glass-panel" style={{ marginTop: '24px', background: 'linear-gradient(to right, rgba(139, 92, 246, 0.1), rgba(236, 72, 153, 0.1))' }}>
        <h2>Need Financial Assistance?</h2>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '24px' }}>Apply for the MOE Financial Assistance Scheme with our AI assistant.</p>
        <Link href="/portal/fas" className="btn-primary" style={{ textDecoration: 'none' }}>Start Application</Link>
      </div>
    </div>
  );
}
