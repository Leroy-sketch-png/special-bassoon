"use client";

import { useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "@/lib/msal";
import { useRouter } from "next/navigation";
import { api } from "@/lib/apiClient";

export default function AdminDashboard() {
  const { instance, accounts: msalAccounts } = useMsal();
  const router = useRouter();
  const [adminInfo, setAdminInfo] = useState<{ userId: string; name: string; email: string; roles: string[] } | null>(null);
  
  // Real accounts fetched from backend
  const [citizenAccounts, setCitizenAccounts] = useState<any[]>([]);
  
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (msalAccounts.length === 0) {
      router.push("/admin/login");
      return;
    }

    const fetchAdminData = async () => {
      try {
        let token = null;
        if (!token && msalAccounts.length > 0) {
          const response = await instance.acquireTokenSilent({
            ...loginRequest,
            account: msalAccounts[0],
          });
          token = response.accessToken;
        }

        if (!token) throw new Error("No admin token found");
        
        // 1. Verify roles
        const data = await api.admin.me(token);
        setAdminInfo(data);

        // 2. Fetch all accounts
        const accountsData = await api.admin.accounts.list(token);
        setCitizenAccounts(accountsData);

      } catch (err: unknown) {
        console.error(err);
        const errorMessage = err instanceof Error ? err.message : "Failed to authenticate or fetch data.";
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    };

    fetchAdminData();
  }, [msalAccounts, instance, router]);

  if (loading) {
    return <div className="flex-center" style={{ minHeight: '60vh' }}>Loading admin dashboard...</div>;
  }

  if (error || !adminInfo) {
    return (
      <div className="glass-panel" style={{ textAlign: 'center', marginTop: '40px', maxWidth: '600px', margin: '40px auto' }}>
        <h2 style={{ color: 'var(--accent-color)' }}>Access Denied</h2>
        <p style={{ color: 'var(--text-secondary)' }}>{error || "You do not have the required admin roles."}</p>
        <button className="btn-primary" style={{ marginTop: '16px' }} onClick={() => instance.logoutRedirect()}>Sign Out</button>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', marginBottom: '32px' }}>
        <div>
          <h1 className="page-title" style={{ marginBottom: '8px' }}>Admin Dashboard</h1>
          <p className="page-subtitle">Welcome, {adminInfo.name} ({adminInfo.roles.join(', ')})</p>
        </div>
        <div style={{ display: 'flex', gap: '12px' }}>
          <button className="btn-primary" style={{ background: 'transparent', border: '1px solid var(--accent-color)' }} onClick={() => router.push('/admin/billing')}>Manage Billing & Courses</button>
          <button className="btn-primary" style={{ background: 'transparent', border: '1px solid var(--accent-color)' }} onClick={() => router.push('/admin/fas')}>Review FAS Applications</button>
        </div>
      </div>

      <div className="glass-panel">
        <h2 style={{ marginTop: 0 }}>Education Accounts Overview</h2>
        
        <table style={{ width: '100%', textAlign: 'left', marginTop: '24px', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid var(--glass-border)' }}>
              <th style={{ padding: '12px' }}>NRIC</th>
              <th style={{ padding: '12px' }}>Name</th>
              <th style={{ padding: '12px' }}>Age</th>
              <th style={{ padding: '12px' }}>Status</th>
              <th style={{ padding: '12px' }}>Balance</th>
              <th style={{ padding: '12px' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {citizenAccounts.map(acc => (
              <tr key={acc.id} style={{ borderBottom: '1px solid var(--glass-border)' }}>
                <td style={{ padding: '12px' }}>{acc.nric}</td>
                <td style={{ padding: '12px' }}>{acc.fullName}</td>
                <td style={{ padding: '12px' }}>{acc.age}</td>
                <td style={{ padding: '12px', color: acc.educationAccount?.status === 'Active' ? '#10b981' : 'var(--text-secondary)' }}>
                  {acc.educationAccount?.status}
                </td>
                <td style={{ padding: '12px' }}>S$ {acc.educationAccount?.balance.toFixed(2)}</td>
                <td style={{ padding: '12px' }}>
                  <button 
                    className="btn-primary" 
                    style={{ padding: '6px 12px', fontSize: '0.85rem', background: 'transparent', border: '1px solid var(--accent-color)', color: 'var(--text-color)' }}
                    onClick={() => router.push(`/admin/accounts/${acc.id}`)}
                  >
                    Manage
                  </button>
                </td>
              </tr>
            ))}
            {citizenAccounts.length === 0 && (
              <tr>
                <td colSpan={6} style={{ padding: '16px', textAlign: 'center', color: 'var(--text-secondary)' }}>No accounts found.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
