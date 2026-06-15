"use client";

import { useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "@/lib/msal";
import { useRouter } from "next/navigation";
import { api, CitizenRecord, EducationAccountTransaction } from "@/lib/apiClient";
import Link from "next/link";

export default function AdminAccountDetails({ params }: { params: { id: string } }) {
  const { instance, accounts } = useMsal();
  const router = useRouter();
  
  const [record, setRecord] = useState<CitizenRecord | null>(null);
  const [transactions, setTransactions] = useState<EducationAccountTransaction[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  // Modal states
  const [showTopUp, setShowTopUp] = useState(false);
  const [topUpAmount, setTopUpAmount] = useState("");
  const [topUpReason, setTopUpReason] = useState("");

  const [showOverride, setShowOverride] = useState(false);
  const [overrideStatus, setOverrideStatus] = useState("Active");
  const [overrideReason, setOverrideReason] = useState("");
  
  const [showCreate, setShowCreate] = useState(false);
  const [createReason, setCreateReason] = useState("");
  
  const [actionLoading, setActionLoading] = useState(false);

  const getAdminToken = async () => {

    if (accounts.length > 0) {
      const response = await instance.acquireTokenSilent({ ...loginRequest, account: accounts[0] });
      return response.accessToken;
    }
    return null;
  };

  const fetchAccountData = async () => {
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");
      const data = await api.admin.accounts.get(params.id, token);
      setRecord(data.record);
      setTransactions(data.transactions);
    } catch (err: unknown) {
      console.error(err);
      setError("Failed to fetch account details.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (accounts.length === 0) {
      router.push("/admin/login");
      return;
    }
    fetchAccountData();
  }, [accounts, instance, router, params.id]);

  const handleTopUp = async (e: React.FormEvent) => {
    e.preventDefault();
    setActionLoading(true);
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");
      await api.admin.accounts.topUp(params.id, parseFloat(topUpAmount), topUpReason, token);
      setShowTopUp(false);
      setTopUpAmount("");
      setTopUpReason("");
      await fetchAccountData();
    } catch (err: any) {
      alert(err.message || "Failed to top up");
    } finally {
      setActionLoading(false);
    }
  };

  const handleOverride = async (e: React.FormEvent) => {
    e.preventDefault();
    setActionLoading(true);
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");
      await api.admin.accounts.overrideStatus(params.id, overrideStatus, overrideReason, token);
      setShowOverride(false);
      setOverrideReason("");
      await fetchAccountData();
    } catch (err: any) {
      alert(err.message || "Failed to override status");
    } finally {
      setActionLoading(false);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setActionLoading(true);
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");
      await api.admin.accounts.create(params.id, createReason, token);
      setShowCreate(false);
      setCreateReason("");
      await fetchAccountData();
    } catch (err: any) {
      alert(err.message || "Failed to create account");
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) return <div className="flex-center" style={{ minHeight: '60vh' }}>Loading details...</div>;
  if (error || !record) return <div className="glass-panel" style={{ textAlign: 'center' }}><h2>Error</h2><p>{error}</p></div>;

  return (
    <div style={{ maxWidth: '1000px', margin: '0 auto', position: 'relative' }}>
      <Link href="/admin" style={{ color: 'var(--accent-color)', textDecoration: 'none', marginBottom: '16px', display: 'inline-block' }}>&larr; Back to Dashboard</Link>
      
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '24px' }}>
        <div>
          <h1 className="page-title" style={{ marginBottom: '8px' }}>Account Details</h1>
          <p className="page-subtitle">{record.fullName} ({record.nric})</p>
        </div>
        <div style={{ display: 'flex', gap: '12px' }}>
          {record.educationAccount?.status === 'NotYetCreated' ? (
            <button className="btn-primary" onClick={() => setShowCreate(true)}>Create Account</button>
          ) : (
            <>
              <button className="btn-primary" style={{ background: 'transparent', border: '1px solid var(--accent-color)' }} onClick={() => setShowOverride(true)}>Override Status</button>
              <button className="btn-primary" onClick={() => setShowTopUp(true)}>Manual Top-Up</button>
            </>
          )}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '24px', marginBottom: '32px' }}>
        <div className="glass-panel">
          <h3>Profile Information</h3>
          <div style={{ marginTop: '16px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-secondary)' }}>Date of Birth</span>
              <span>{record.dateOfBirth}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-secondary)' }}>Education Account</span>
              <span style={{ color: record.educationAccount?.status === 'Active' ? '#10b981' : 'var(--text-secondary)' }}>{record.educationAccount?.status}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-secondary)' }}>Opened Date</span>
              <span>{record.educationAccount?.openedDate || 'N/A'}</span>
            </div>
            {record.educationAccount?.status === 'Closed' && (
              <>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Closed Date</span>
                  <span>{record.educationAccount?.closedDate}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Closure Reason</span>
                  <span>{record.educationAccount?.closureReason}</span>
                </div>
              </>
            )}
          </div>
        </div>

        <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center' }}>
          <h3 style={{ color: 'var(--text-secondary)' }}>Current Balance</h3>
          <div style={{ fontSize: '3rem', fontWeight: 'bold', margin: '16px 0' }}>
            S$ {record.educationAccount?.balance.toFixed(2)}
          </div>
        </div>
      </div>

      <div className="glass-panel">
        <h3 style={{ marginBottom: '16px' }}>Ledger & Audit Trail</h3>
        {transactions.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)' }}>No transactions found for this account.</p>
        ) : (
          <table style={{ width: '100%', textAlign: 'left', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid var(--glass-border)' }}>
                <th style={{ padding: '12px' }}>Date</th>
                <th style={{ padding: '12px' }}>Type</th>
                <th style={{ padding: '12px' }}>Description</th>
                <th style={{ padding: '12px', textAlign: 'right' }}>Amount</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map(t => (
                <tr key={t.id} style={{ borderBottom: '1px solid var(--glass-border)' }}>
                  <td style={{ padding: '12px' }}>{new Date(t.transactionDate).toLocaleString()}</td>
                  <td style={{ padding: '12px' }}>
                    <span style={{ padding: '4px 8px', background: 'rgba(255,255,255,0.1)', borderRadius: '4px', fontSize: '0.85rem' }}>{t.transactionType}</span>
                  </td>
                  <td style={{ padding: '12px', color: 'var(--text-secondary)' }}>{t.description}</td>
                  <td style={{ padding: '12px', textAlign: 'right', color: t.amount > 0 ? '#10b981' : (t.amount < 0 ? '#ef4444' : 'var(--text-color)') }}>
                    {t.amount > 0 ? '+' : ''}{t.amount.toFixed(2)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Top Up Modal */}
      {showTopUp && (
        <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div className="glass-panel" style={{ width: '400px' }}>
            <h2 style={{ marginTop: 0 }}>Manual Top-Up</h2>
            <form onSubmit={handleTopUp} style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
              <div>
                <label>Amount (S$)</label>
                <input type="number" step="0.01" min="0.01" required value={topUpAmount} onChange={e => setTopUpAmount(e.target.value)} className="input-field" />
              </div>
              <div>
                <label>Reason</label>
                <textarea required value={topUpReason} onChange={e => setTopUpReason(e.target.value)} className="input-field" rows={3}></textarea>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '8px' }}>
                <button type="button" className="btn-primary" style={{ background: 'transparent', border: 'none' }} onClick={() => setShowTopUp(false)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={actionLoading}>{actionLoading ? 'Processing...' : 'Top Up'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Override Modal */}
      {showOverride && (
        <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div className="glass-panel" style={{ width: '400px' }}>
            <h2 style={{ marginTop: 0 }}>Override Account Status</h2>
            <form onSubmit={handleOverride} style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
              <div>
                <label>New Status</label>
                <select value={overrideStatus} onChange={e => setOverrideStatus(e.target.value)} className="input-field">
                  <option value="Active">Active</option>
                  <option value="Closed">Closed</option>
                  <option value="Suspended">Suspended</option>
                </select>
              </div>
              <div>
                <label>Reason</label>
                <textarea required value={overrideReason} onChange={e => setOverrideReason(e.target.value)} className="input-field" rows={3}></textarea>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '8px' }}>
                <button type="button" className="btn-primary" style={{ background: 'transparent', border: 'none' }} onClick={() => setShowOverride(false)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={actionLoading}>{actionLoading ? 'Processing...' : 'Update Status'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create Modal */}
      {showCreate && (
        <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div className="glass-panel" style={{ width: '400px' }}>
            <h2 style={{ marginTop: 0 }}>Create Account Manually</h2>
            <form onSubmit={handleCreate} style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
              <div>
                <label>Reason</label>
                <textarea required value={createReason} onChange={e => setCreateReason(e.target.value)} className="input-field" rows={3}></textarea>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '8px' }}>
                <button type="button" className="btn-primary" style={{ background: 'transparent', border: 'none' }} onClick={() => setShowCreate(false)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={actionLoading}>{actionLoading ? 'Processing...' : 'Create Account'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

    </div>
  );
}
