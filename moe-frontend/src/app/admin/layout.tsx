"use client";

import { useEffect, useState } from 'react';
import { useMsal } from "@azure/msal-react";
import Link from 'next/link';

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const { instance, accounts } = useMsal();
  const [devToken, setDevToken] = useState<string | null>(null);

  useEffect(() => {
    // Check for dev token
    const token = null;
    if (token) setDevToken(token);
  }, []);

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <header className="flex-between" style={{ padding: '16px 32px', background: 'var(--glass-bg)', borderBottom: '1px solid var(--glass-border)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '32px' }}>
          <div style={{ fontSize: '1.2rem', fontWeight: 'bold', color: 'var(--accent-color)' }}>MOE Admin Portal</div>
          <nav style={{ display: 'flex', gap: '16px' }}>
            <Link href="/admin" style={{ color: 'var(--text-color)', textDecoration: 'none', fontWeight: 500 }}>Accounts</Link>
            <Link href="/admin/billing" style={{ color: 'var(--text-color)', textDecoration: 'none', fontWeight: 500 }}>Billing</Link>
            <Link href="/admin/fas" style={{ color: 'var(--text-color)', textDecoration: 'none', fontWeight: 500 }}>FAS Review</Link>
          </nav>
        </div>
        <div>
          {accounts.length > 0 || devToken ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
              <span style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                {accounts.length > 0 ? accounts[0].name : "Dev Admin"}
              </span>
              <button 
                className="btn-primary" 
                style={{ padding: '8px 16px' }} 
                onClick={() => {
                  if (accounts.length > 0) instance.logoutRedirect();
                  else {

                    window.location.href = "/admin/login";
                  }
                }}
              >
                Sign Out
              </button>
            </div>
          ) : (
            <Link href="/admin/login" className="btn-primary" style={{ padding: '8px 16px', textDecoration: 'none' }}>Sign In</Link>
          )}
        </div>
      </header>
      <main style={{ flex: 1, padding: '32px', background: 'var(--bg-color)' }}>
        {children}
      </main>
    </div>
  );
}


