"use client";

import { useMsal } from "@azure/msal-react";
import { loginRequest } from "@/lib/msal";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

export default function AdminLogin() {
  const { instance, accounts } = useMsal();
  const router = useRouter();

  useEffect(() => {
    if (accounts.length > 0) {
      router.push("/admin");
    }
  }, [accounts, router]);

  const handleLogin = () => {
    instance.loginRedirect(loginRequest).catch(e => {
      console.error(e);
    });
  };


  return (
    <div className="flex-center" style={{ height: '80vh' }}>
      <div className="glass-panel" style={{ width: '400px', textAlign: 'center' }}>
        <h2 style={{ color: 'var(--accent-color)', marginBottom: '16px' }}>HQ/School Admin Login</h2>
        <p style={{ color: 'var(--text-secondary)', marginBottom: '32px' }}>Sign in with your MOE Entra ID (Microsoft) account.</p>
        <button className="btn-primary" style={{ width: '100%', padding: '16px', fontSize: '1.1rem' }} onClick={handleLogin}>
          Sign in with Microsoft
        </button>
      </div>
    </div>
  );
}
