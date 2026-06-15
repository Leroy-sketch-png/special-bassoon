"use client";

import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { api, CitizenRecord, Invoice } from '@/lib/apiClient';

import { Suspense } from 'react';

function PaymentPageContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const invoiceId = searchParams.get('invoiceId');

  const [profile, setProfile] = useState<CitizenRecord | null>(null);
  const [invoice, setInvoice] = useState<Invoice | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  const [intentLoading, setIntentLoading] = useState(false);

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

    Promise.all([
      api.eligibility.me(token),
      api.payments.getInvoice(invoiceId, token)
    ])
      .then(([profileData, invoiceData]) => {
        setProfile(profileData);
        setInvoice(invoiceData);
      })
      .catch((err) => {
        setError(err.message || "Failed to load payment data");
      })
      .finally(() => {
        setLoading(false);
      });
  }, [router, invoiceId]);

  const handlePayNow = async () => {
    if (!invoiceId) return;
    setIntentLoading(true);

    try {
      const token = document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1] || '';
      
      const result = await api.payments.createIntent(invoiceId, "testpayer@example.com", token);
      
      if (result.requiresPspPayment && result.checkoutUrl) {
        // Redirect to HitPay
        window.location.href = result.checkoutUrl;
      } else {
        // Fully covered by Education Account
        router.push(`/portal/payments/return?invoice=${invoiceId}`);
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "Failed to initiate payment";
      setError(errorMessage);
      setIntentLoading(false);
    }
  };

  if (loading) return <div className="flex-center" style={{ minHeight: '40vh' }}>Loading...</div>;

  if (error || !invoice || !profile) {
    return (
      <div className="glass-panel" style={{ textAlign: 'center', marginTop: '40px' }}>
        <h2 style={{ color: 'var(--accent-color)' }}>Payment Error</h2>
        <p style={{ color: 'var(--text-secondary)' }}>{error || "Could not load invoice."}</p>
        <button className="btn-primary" onClick={() => router.push('/portal')} style={{ marginTop: '16px' }}>Back to Portal</button>
      </div>
    );
  }

  // Calculate split locally for preview (Backend does real auth check)
  const availableEa = profile.educationAccount?.status === 'Active' ? profile.educationAccount?.balance : 0;
  const eaPortion = Math.min(availableEa, invoice.totalAmount);
  const pspPortion = invoice.totalAmount - eaPortion;

  return (
    <div style={{ maxWidth: '600px', margin: '0 auto' }}>
      <h1 className="page-title">Make a Payment</h1>
      
      <div className="glass-panel" style={{ marginTop: '32px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: '24px', borderBottom: '1px solid var(--glass-border)', paddingBottom: '16px' }}>
          <div>
            <h2 style={{ margin: 0 }}>Invoice {invoice.invoiceNumber}</h2>
            <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)', marginTop: '4px' }}>Issued: {new Date(invoice.issuedAt).toLocaleDateString()}</div>
          </div>
          <div style={{ fontSize: '2rem', fontWeight: 'bold' }}>S$ {invoice.totalAmount.toFixed(2)}</div>
        </div>

        <div style={{ marginBottom: '24px' }}>
          {invoice.lineItems && invoice.lineItems.length > 0 && (
            <div style={{ marginBottom: '24px' }}>
              <h3 style={{ fontSize: '1.1rem', marginBottom: '16px' }}>Invoice Details</h3>
              {invoice.lineItems.map(item => (
                <div key={item.id} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', paddingBottom: '8px', borderBottom: '1px dashed var(--glass-border)' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>{item.description}</span>
                  <span>S$ {item.amount.toFixed(2)}</span>
                </div>
              ))}
            </div>
          )}
          
          <h3 style={{ fontSize: '1.1rem', marginBottom: '16px' }}>Payment Split Breakdown</h3>
          
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '12px' }}>
            <span style={{ color: 'var(--text-secondary)' }}>Total Amount</span>
            <span>S$ {invoice.totalAmount.toFixed(2)}</span>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '12px', color: '#10b981' }}>
            <span>Deduct from Education Account (Bal: S$ {profile.educationAccount?.balance.toFixed(2)})</span>
            <span>- S$ {eaPortion.toFixed(2)}</span>
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '16px', paddingTop: '16px', borderTop: '1px solid var(--glass-border)', fontWeight: 'bold', fontSize: '1.2rem' }}>
            <span>Pay via PayNow / Card</span>
            <span>S$ {pspPortion.toFixed(2)}</span>
          </div>
        </div>

        {invoice.status === 'Paid' ? (
          <div style={{ textAlign: 'center', padding: '16px', background: 'rgba(16, 185, 129, 0.1)', color: '#10b981', borderRadius: '8px', fontWeight: 'bold' }}>
            This invoice is already paid.
          </div>
        ) : (
          <button 
            className="btn-primary" 
            style={{ width: '100%', padding: '16px', fontSize: '1.1rem' }} 
            onClick={handlePayNow}
            disabled={intentLoading}
          >
            {intentLoading ? 'Processing...' : (pspPortion > 0 ? 'Proceed to HitPay Checkout' : 'Confirm Education Account Payment')}
          </button>
        )}
      </div>
    </div>
  );
}

export default function PaymentPage() {
  return (
    <Suspense fallback={<div className="flex-center" style={{ minHeight: '40vh' }}>Loading...</div>}>
      <PaymentPageContent />
    </Suspense>
  );
}
