"use client";

import { useState } from 'react';
import { useRouter } from 'next/navigation';

export default function Login() {
  const [nric, setNric] = useState('S1234567A');
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  const handleLogin = async () => {
    setLoading(true);
    try {
      const res = await fetch('http://localhost:5006/api/auth/mock-singpass-login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Nric: nric })
      });
      const data = await res.json();
      if (data.token) {
        document.cookie = `token=${data.token}; path=/;`;
        router.push('/portal');
      } else {
        alert("Login failed");
      }
    } catch {
      alert("Error logging in");
    }
    setLoading(false);
  };

  return (
    <div className="flex-center" style={{ height: '80vh' }}>
      <div className="glass-panel" style={{ width: '400px', textAlign: 'center' }}>
        <h2>Singpass Login (Mock)</h2>
        <div style={{ margin: '24px 0', textAlign: 'left' }}>
          <label style={{ display: 'block', marginBottom: '8px', color: 'var(--text-secondary)' }}>NRIC</label>
          <input 
            type="text" 
            className="input-field" 
            value={nric} 
            onChange={e => setNric(e.target.value)} 
          />
        </div>
        <button className="btn-primary" style={{ width: '100%' }} onClick={handleLogin} disabled={loading}>
          {loading ? 'Authenticating...' : 'Login'}
        </button>
      </div>
    </div>
  );
}
