"use client";

import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { api, Invoice } from '@/lib/apiClient';

import { Suspense } from 'react';

function PaymentReturnPageContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const invoiceId = searchParams.get('invoice');

  const [invoice, setInvoice] = useState<Invoice | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!invoiceId) {
      router.push('/portal');
      return;
    }

    const token = document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1];
    if (!token) {
      router.push('/login');
      return;
    }

    // HitPay webhook might take a second to process.
    // In a real app, we might poll this a few times, but for now we'll do a simple delay/fetch.
    const checkInvoice = async () => {
      try {
        const data = await api.payments.verifyPayment(invoiceId, token);
        setInvoice(data);
      } catch (err: unknown) {
        const errorMessage = err instanceof Error ? err.message : "Failed to load invoice status";
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    };

    const timer = setTimeout(checkInvoice, 1000);
    return () => clearTimeout(timer);

  }, [router, invoiceId]);

  if (loading) return <div className="flex-center" style={{ minHeight: '40vh' }}>Checking payment status...</div>;

  if (error || !invoice) {
    return (
      <div className="glass-panel" style={{ textAlign: 'center', marginTop: '40px', maxWidth: '500px', margin: '40px auto' }}>
        <h2 style={{ color: 'var(--accent-color)' }}>Error</h2>
        <p style={{ color: 'var(--text-secondary)' }}>{error || "Could not load invoice."}</p>
        <Link href="/portal" className="btn-primary" style={{ display: 'inline-block', marginTop: '16px', textDecoration: 'none' }}>Back to Portal</Link>
      </div>
    );
  }

  const isPaid = invoice.status === 'Paid';

  return (
    <div style={{ maxWidth: '600px', margin: '0 auto', textAlign: 'center', paddingTop: '40px' }}>
      
      {isPaid ? (
        <div className="glass-panel" style={{ padding: '40px' }}>
          <div style={{ fontSize: '4rem', marginBottom: '16px' }}>✅</div>
          <h2 style={{ marginBottom: '8px' }}>Payment Successful</h2>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '32px' }}>
            Invoice {invoice.invoiceNumber} has been marked as Paid.
          </p>
          <div style={{ background: 'var(--bg-color)', padding: '16px', borderRadius: '8px', border: '1px solid var(--glass-border)', textAlign: 'left', marginBottom: '32px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
              <span style={{ color: 'var(--text-secondary)' }}>Total Amount</span>
              <span style={{ fontWeight: 'bold' }}>S$ {invoice.totalAmount.toFixed(2)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
              <span style={{ color: 'var(--text-secondary)' }}>From Education Account</span>
              <span>S$ {(invoice.educationAccountPortion || 0).toFixed(2)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-secondary)' }}>From PSP (PayNow/Card)</span>
              <span>S$ {(invoice.externalPspPortion || 0).toFixed(2)}</span>
            </div>
          </div>
          <Link href="/portal" className="btn-primary" style={{ textDecoration: 'none' }}>Return to Dashboard</Link>
        </div>
      ) : (
        <div className="glass-panel" style={{ padding: '40px' }}>
          <div style={{ fontSize: '4rem', marginBottom: '16px' }}>⏳</div>
          <h2 style={{ marginBottom: '8px' }}>Payment Pending</h2>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '32px' }}>
            We are waiting for confirmation from the payment provider. Your invoice status will update automatically once processed.
          </p>
          <Link href="/portal" className="btn-primary" style={{ textDecoration: 'none' }}>Return to Dashboard</Link>
        </div>
      )}
      
    </div>
  );
}

export default function PaymentReturnPage() {
  return (
    <Suspense fallback={<div className="flex-center" style={{ minHeight: '40vh' }}>Loading...</div>}>
      <PaymentReturnPageContent />
    </Suspense>
  );
}
