"use client";

import { useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "@/lib/msal";
import { useRouter } from "next/navigation";
import { api } from "@/lib/apiClient";
import Link from "next/link";

export default function AdminFasPage() {
  const { instance, accounts } = useMsal();
  const router = useRouter();

  const [applications, setApplications] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const getAdminToken = async () => {

    if (accounts.length > 0) {
      const response = await instance.acquireTokenSilent({ ...loginRequest, account: accounts[0] });
      return response.accessToken;
    }
    return null;
  };

  useEffect(() => {
    if (accounts.length === 0) {
      router.push("/admin/login");
      return;
    }

    const fetchFasData = async () => {
      try {
        const token = await getAdminToken();
        if (!token) throw new Error("No admin token found");
        
        const data = await api.fas.list(token);
        setApplications(data);
      } catch (err: unknown) {
        console.error(err);
        setError("Failed to fetch FAS applications.");
      } finally {
        setLoading(false);
      }
    };

    fetchFasData();
  }, [accounts, instance, router]);

  if (loading) return <div className="flex-center" style={{ minHeight: '60vh' }}>Loading...</div>;
  if (error) return <div className="glass-panel" style={{ textAlign: 'center' }}><h2>Error</h2><p>{error}</p></div>;

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', marginBottom: '32px' }}>
        <div>
          <h1 className="page-title" style={{ marginBottom: '8px' }}>FAS Applications</h1>
          <p className="page-subtitle">Review Financial Assistance Scheme applications</p>
        </div>
      </div>

      <div className="glass-panel">
        <h2 style={{ marginTop: 0 }}>Recent Applications</h2>
        
        <table style={{ width: '100%', textAlign: 'left', marginTop: '24px', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid var(--glass-border)' }}>
              <th style={{ padding: '12px' }}>Applicant</th>
              <th style={{ padding: '12px' }}>NRIC</th>
              <th style={{ padding: '12px' }}>Submitted At</th>
              <th style={{ padding: '12px' }}>Status</th>
              <th style={{ padding: '12px' }}>Preliminary Tier</th>
              <th style={{ padding: '12px' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {applications.map(app => {
              const isEligible = app.preliminaryTier === "Eligible";
              
              return (
                <tr key={app.id} style={{ borderBottom: '1px solid var(--glass-border)' }}>
                  <td style={{ padding: '12px' }}>{app.citizenName}</td>
                  <td style={{ padding: '12px' }}>{app.citizenNric}</td>
                  <td style={{ padding: '12px' }}>{new Date(app.submittedAt).toLocaleString()}</td>
                  <td style={{ padding: '12px' }}>
                    <span style={{ 
                      color: app.status === 'Approved' ? '#10b981' : (app.status === 'Rejected' ? '#ef4444' : '#f59e0b') 
                    }}>
                      {app.status}
                    </span>
                  </td>
                  <td style={{ padding: '12px' }}>
                    <span style={{ color: isEligible ? '#10b981' : '#ef4444', fontSize: '0.9rem' }}>
                      {app.preliminaryTier}
                    </span>
                  </td>
                  <td style={{ padding: '12px' }}>
                    <button 
                      className="btn-primary" 
                      style={{ padding: '6px 12px', fontSize: '0.85rem', background: 'transparent', border: '1px solid var(--accent-color)', color: 'var(--text-color)' }}
                      onClick={() => router.push(`/admin/fas/${app.id}`)}
                    >
                      Review
                    </button>
                  </td>
                </tr>
              );
            })}
            {applications.length === 0 && (
              <tr>
                <td colSpan={5} style={{ padding: '16px', textAlign: 'center', color: 'var(--text-secondary)' }}>No applications found.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
