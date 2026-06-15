"use client";

import { useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "@/lib/msal";
import { useRouter } from "next/navigation";
import { api } from "@/lib/apiClient";
import Link from "next/link";

export default function AdminFasDetails({ params }: { params: { id: string } }) {
  const { instance, accounts } = useMsal();
  const router = useRouter();

  const [application, setApplication] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [reviewStatus, setReviewStatus] = useState("Approved");
  const [remarks, setRemarks] = useState("");
  const [actionLoading, setActionLoading] = useState(false);

  const getAdminToken = async () => {

    if (accounts.length > 0) {
      const response = await instance.acquireTokenSilent({ ...loginRequest, account: accounts[0] });
      return response.accessToken;
    }
    return null;
  };

  const fetchFasData = async () => {
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");
      const data = await api.fas.get(params.id, token);
      setApplication(data);
    } catch (err: unknown) {
      console.error(err);
      setError("Failed to fetch FAS application details.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (accounts.length === 0) {
      router.push("/admin/login");
      return;
    }
    fetchFasData();
  }, [accounts, instance, router, params.id]);

  const handleReview = async (e: React.FormEvent) => {
    e.preventDefault();
    setActionLoading(true);
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");
      await api.fas.review(params.id, reviewStatus, remarks, token);
      await fetchFasData();
    } catch (err: any) {
      alert(err.message || "Failed to submit review");
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) return <div className="flex-center" style={{ minHeight: '60vh' }}>Loading...</div>;
  if (error || !application) return <div className="glass-panel" style={{ textAlign: 'center' }}><h2>Error</h2><p>{error}</p></div>;

  const data = JSON.parse(application.applicationDataJson || "{}");

  return (
    <div style={{ maxWidth: '1000px', margin: '0 auto' }}>
      <Link href="/admin/fas" style={{ color: 'var(--accent-color)', textDecoration: 'none', marginBottom: '16px', display: 'inline-block' }}>&larr; Back to Applications</Link>
      
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '24px' }}>
        <div>
          <h1 className="page-title" style={{ marginBottom: '8px' }}>FAS Application Details</h1>
          <p className="page-subtitle">{application.citizenRecord.fullName} ({application.citizenRecord.nric})</p>
        </div>
        <div>
          <span style={{ 
            padding: '8px 16px', borderRadius: '24px', fontWeight: 'bold',
            background: application.status === 'Approved' ? 'rgba(16, 185, 129, 0.2)' : (application.status === 'Rejected' ? 'rgba(239, 68, 68, 0.2)' : 'rgba(245, 158, 11, 0.2)'),
            color: application.status === 'Approved' ? '#10b981' : (application.status === 'Rejected' ? '#ef4444' : '#f59e0b') 
          }}>
            {application.status}
          </span>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '24px' }}>
        
        <div className="glass-panel">
          <h2 style={{ marginTop: 0 }}>Application Data</h2>
          
          {(() => {
            const income = Number(data.household_income || 0);
            const size = Number(data.household_size || 1);
            const pci = income / size;
            const isEligible = income <= 3000 || pci <= 750;
            return (
              <div style={{ marginTop: '16px', padding: '12px', borderRadius: '8px', borderLeft: '4px solid', borderColor: isEligible ? '#10b981' : '#ef4444', background: isEligible ? 'rgba(16, 185, 129, 0.1)' : 'rgba(239, 68, 68, 0.1)' }}>
                <strong>Preliminary Eligibility Tier: </strong>
                {isEligible ? "Eligible for FAS (Income <= $3000 or PCI <= $750)" : "Not Eligible (Exceeds Income Cap)"}
                <br/>
                <span style={{ fontSize: '0.85rem' }}>PCI: S$ {pci.toFixed(2)} / month</span>
              </div>
            );
          })()}

          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '24px' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Household Income</label>
                <div style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>S$ {data.household_income}</div>
              </div>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Household Size</label>
                <div style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>{data.household_size}</div>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Number of Dependants</label>
                <div style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>{data.num_dependants}</div>
              </div>
              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Year of Study</label>
                <div style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>{data.year_of_study}</div>
              </div>
            </div>

            <div>
              <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>School Name</label>
              <div style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>{data.school_name}</div>
            </div>

            <div>
              <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Reason for Application</label>
              <div style={{ padding: '12px', background: 'rgba(0,0,0,0.2)', borderRadius: '8px' }}>
                {data.reason_for_application}
              </div>
            </div>
            
            <div style={{ marginTop: '16px', padding: '12px', borderLeft: '4px solid #10b981', background: 'rgba(16, 185, 129, 0.05)' }}>
              <strong>Declarations:</strong> The applicant has consented to data retrieval and declared all information to be true.
            </div>
          </div>
        </div>

        <div className="glass-panel">
          <h2 style={{ marginTop: 0 }}>Review Action</h2>
          
          {application.status === 'Approved' || application.status === 'Rejected' ? (
            <div>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Reviewed on: {new Date(application.reviewedAt).toLocaleString()}</p>
              <div style={{ marginTop: '16px' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Admin Remarks</label>
                <p>{application.adminRemarks || 'No remarks provided.'}</p>
              </div>
            </div>
          ) : application.status === 'PendingApproval' ? (
            <div>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Maker has submitted a review. Awaiting Checker approval.</p>
              <div style={{ marginTop: '16px' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Maker Remarks</label>
                <p>{application.adminRemarks || 'No remarks provided.'}</p>
              </div>

              <form onSubmit={async (e) => {
                e.preventDefault();
                setActionLoading(true);
                try {
                  const token = await getAdminToken();
                  await api.fas.approve(params.id, reviewStatus, remarks, token as string);
                  await fetchFasData();
                } catch (err: any) {
                  alert(err.message || "Failed to submit approval");
                } finally {
                  setActionLoading(false);
                }
              }} style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px', borderTop: '1px solid var(--glass-border)', paddingTop: '16px' }}>
                <h3>Checker Action</h3>
                <div>
                  <label>Decision</label>
                  <select value={reviewStatus} onChange={e => setReviewStatus(e.target.value)} className="input-field">
                    <option value="Approved">Approve</option>
                    <option value="Rejected">Reject</option>
                  </select>
                </div>
                <div>
                  <label>Checker Remarks</label>
                  <textarea value={remarks} onChange={e => setRemarks(e.target.value)} className="input-field" rows={4} placeholder="Internal review notes..."></textarea>
                </div>
                <button type="submit" className="btn-primary" disabled={actionLoading} style={{ marginTop: '8px' }}>
                  {actionLoading ? 'Submitting...' : 'Submit Decision'}
                </button>
              </form>
            </div>
          ) : (
            <form onSubmit={handleReview} style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
              <div>
                <label>Remarks</label>
                <textarea required value={remarks} onChange={e => setRemarks(e.target.value)} className="input-field" rows={4} placeholder="Internal review notes..."></textarea>
              </div>
              <button type="submit" className="btn-primary" disabled={actionLoading} style={{ marginTop: '8px' }}>
                {actionLoading ? 'Submitting...' : 'Submit to Checker'}
              </button>
            </form>
          )}
        </div>

      </div>
    </div>
  );
}
