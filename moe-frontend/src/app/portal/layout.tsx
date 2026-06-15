"use client";

import Link from 'next/link';

export default function PortalLayout({ children }: { children: React.ReactNode }) {
  return (
    <div>
      <div className="flex-between" style={{ padding: '16px 24px', background: 'var(--glass-bg)', borderRadius: '12px', marginBottom: '32px', border: '1px solid var(--glass-border)' }}>
        <h2 style={{ margin: 0 }}>Student Portal</h2>
        <div style={{ display: 'flex', gap: '24px', alignItems: 'center' }}>
          <Link href="/portal" style={{ color: 'white', textDecoration: 'none', fontWeight: 500 }}>Dashboard</Link>
          <Link href="/portal/fas" style={{ color: 'white', textDecoration: 'none', fontWeight: 500 }}>FAS Application</Link>
          <button className="btn-primary" style={{ padding: '6px 16px', fontSize: '0.9rem' }} onClick={() => {
            document.cookie = 'token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
            window.location.href = '/';
          }}>Logout</button>
        </div>
      </div>
      {children}
    </div>
  );
}
