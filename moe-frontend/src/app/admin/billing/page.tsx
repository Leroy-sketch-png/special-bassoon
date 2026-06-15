"use client";

import { useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "@/lib/msal";
import { useRouter } from "next/navigation";
import { api, CitizenRecord } from "@/lib/apiClient";
import Link from "next/link";

export default function AdminBillingPage() {
  const { instance, accounts } = useMsal();
  const router = useRouter();

  const [courses, setCourses] = useState<any[]>([]);
  const [citizens, setCitizens] = useState<CitizenRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Create Course Form
  const [showCreateCourse, setShowCreateCourse] = useState(false);
  const [courseName, setCourseName] = useState("");
  const [courseDesc, setCourseDesc] = useState("");
  const [feeComponents, setFeeComponents] = useState([{ name: "", amount: 0, isGstApplicable: false }]);
  const [createLoading, setCreateLoading] = useState(false);

  // Enroll Form
  const [showEnroll, setShowEnroll] = useState(false);
  const [enrollCourseId, setEnrollCourseId] = useState("");
  const [enrollCitizenId, setEnrollCitizenId] = useState("");
  const [enrollLoading, setEnrollLoading] = useState(false);

  const getAdminToken = async () => {

    if (accounts.length > 0) {
      const response = await instance.acquireTokenSilent({ ...loginRequest, account: accounts[0] });
      return response.accessToken;
    }
    return null;
  };

  const fetchBillingData = async () => {
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");

      const [coursesData, citizensData] = await Promise.all([
        api.admin.billing.getCourses(token),
        api.admin.accounts.list(token)
      ]);

      setCourses(coursesData);
      setCitizens(citizensData);
    } catch (err: unknown) {
      console.error(err);
      setError("Failed to fetch billing data.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (accounts.length === 0) {
      router.push("/admin/login");
      return;
    }
    fetchBillingData();
  }, [accounts, instance, router]);

  const handleCreateCourse = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreateLoading(true);
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");
      await api.admin.billing.createCourse(courseName, courseDesc, feeComponents, token);
      
      setShowCreateCourse(false);
      setCourseName("");
      setCourseDesc("");
      setFeeComponents([{ name: "", amount: 0, isGstApplicable: false }]);
      await fetchBillingData();
    } catch (err: any) {
      alert(err.message || "Failed to create course");
    } finally {
      setCreateLoading(false);
    }
  };

  const handleEnroll = async (e: React.FormEvent) => {
    e.preventDefault();
    setEnrollLoading(true);
    try {
      const token = await getAdminToken();
      if (!token) throw new Error("No admin token found");
      await api.admin.billing.enroll(enrollCourseId, enrollCitizenId, token);
      
      setShowEnroll(false);
      setEnrollCourseId("");
      setEnrollCitizenId("");
      alert("Student enrolled successfully. Invoice has been generated.");
    } catch (err: any) {
      alert(err.message || "Failed to enroll student");
    } finally {
      setEnrollLoading(false);
    }
  };

  if (loading) return <div className="flex-center" style={{ minHeight: '60vh' }}>Loading...</div>;
  if (error) return <div className="glass-panel" style={{ textAlign: 'center' }}><h2>Error</h2><p>{error}</p></div>;

  return (
    <div style={{ maxWidth: '1000px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '32px' }}>
        <div>
          <h1 className="page-title" style={{ marginBottom: '8px' }}>Billing & Courses</h1>
          <p className="page-subtitle">Manage courses, fees, and enrollments</p>
        </div>
        <div style={{ display: 'flex', gap: '12px' }}>
          <button className="btn-primary" onClick={() => setShowCreateCourse(true)}>Create Course</button>
          <button className="btn-primary" style={{ background: 'transparent', border: '1px solid var(--accent-color)' }} onClick={() => setShowEnroll(true)}>Enroll Student</button>
        </div>
      </div>

      <div className="glass-panel">
        <h2 style={{ marginTop: 0 }}>Available Courses</h2>
        {courses.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)' }}>No courses found. Create one to get started.</p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
            {courses.map(course => (
              <div key={course.id} style={{ padding: '16px', border: '1px solid var(--glass-border)', borderRadius: '8px', background: 'rgba(0,0,0,0.2)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                  <h3 style={{ margin: 0 }}>{course.name}</h3>
                  <span style={{ fontWeight: 'bold' }}>
                    Total: S$ {course.feeComponents.reduce((sum: number, fee: any) => sum + fee.amount, 0).toFixed(2)}
                  </span>
                </div>
                <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginBottom: '16px', marginTop: 0 }}>{course.description}</p>
                
                <div style={{ fontSize: '0.85rem' }}>
                  <div style={{ fontWeight: 'bold', marginBottom: '8px', color: 'var(--text-secondary)' }}>Fee Breakdown:</div>
                  {course.feeComponents.map((fee: any) => (
                    <div key={fee.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '4px 0', borderBottom: '1px dashed var(--glass-border)' }}>
                      <span>{fee.name} {fee.isGstApplicable && '(GST Applicable)'}</span>
                      <span>S$ {fee.amount.toFixed(2)}</span>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Create Course Modal */}
      {showCreateCourse && (
        <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div className="glass-panel" style={{ width: '600px', maxHeight: '90vh', overflowY: 'auto' }}>
            <h2 style={{ marginTop: 0 }}>Create New Course</h2>
            <form onSubmit={handleCreateCourse} style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
              <div>
                <label>Course Name</label>
                <input type="text" required value={courseName} onChange={e => setCourseName(e.target.value)} className="input-field" />
              </div>
              <div>
                <label>Description</label>
                <textarea value={courseDesc} onChange={e => setCourseDesc(e.target.value)} className="input-field" rows={2}></textarea>
              </div>

              <div>
                <label style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  Fee Components
                  <button type="button" onClick={() => setFeeComponents([...feeComponents, { name: "", amount: 0, isGstApplicable: false }])} style={{ background: 'transparent', color: 'var(--accent-color)', border: 'none', cursor: 'pointer' }}>+ Add</button>
                </label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '8px' }}>
                  {feeComponents.map((fee, idx) => (
                    <div key={idx} style={{ display: 'grid', gridTemplateColumns: '2fr 1fr auto', gap: '8px', alignItems: 'center', padding: '8px', background: 'rgba(255,255,255,0.05)', borderRadius: '4px' }}>
                      <input type="text" placeholder="Component Name (e.g. Course Fee)" required value={fee.name} onChange={e => { const newFees = [...feeComponents]; newFees[idx].name = e.target.value; setFeeComponents(newFees); }} className="input-field" />
                      <input type="number" step="0.01" min="0" placeholder="Amount" required value={fee.amount || ""} onChange={e => { const newFees = [...feeComponents]; newFees[idx].amount = parseFloat(e.target.value); setFeeComponents(newFees); }} className="input-field" />
                      <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                        <input type="checkbox" checked={fee.isGstApplicable} onChange={e => { const newFees = [...feeComponents]; newFees[idx].isGstApplicable = e.target.checked; setFeeComponents(newFees); }} />
                        <span style={{ fontSize: '0.8rem' }}>GST</span>
                        <button type="button" style={{ background: 'transparent', color: '#ef4444', border: 'none', marginLeft: '8px', cursor: 'pointer' }} onClick={() => setFeeComponents(feeComponents.filter((_, i) => i !== idx))}>✕</button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '16px' }}>
                <button type="button" className="btn-primary" style={{ background: 'transparent', border: 'none' }} onClick={() => setShowCreateCourse(false)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={createLoading}>{createLoading ? 'Saving...' : 'Create Course'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Enroll Student Modal */}
      {showEnroll && (
        <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
          <div className="glass-panel" style={{ width: '400px' }}>
            <h2 style={{ marginTop: 0 }}>Enroll Student</h2>
            <form onSubmit={handleEnroll} style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
              <div>
                <label>Select Course</label>
                <select required value={enrollCourseId} onChange={e => setEnrollCourseId(e.target.value)} className="input-field">
                  <option value="">-- Select a course --</option>
                  {courses.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
              <div>
                <label>Select Student</label>
                <select required value={enrollCitizenId} onChange={e => setEnrollCitizenId(e.target.value)} className="input-field">
                  <option value="">-- Select a citizen --</option>
                  {citizens.map(c => <option key={c.id} value={c.id}>{c.fullName} ({c.nric})</option>)}
                </select>
              </div>
              
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '16px' }}>
                <button type="button" className="btn-primary" style={{ background: 'transparent', border: 'none' }} onClick={() => setShowEnroll(false)}>Cancel</button>
                <button type="submit" className="btn-primary" disabled={enrollLoading}>{enrollLoading ? 'Enrolling...' : 'Enroll & Generate Bill'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

    </div>
  );
}
