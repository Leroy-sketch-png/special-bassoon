"use client";

import { useEffect, useState } from "react";
import { MsalProvider } from "@azure/msal-react";
import { msalInstance } from "@/lib/msal";

export function MsalProviderWrapper({ children }: { children: React.ReactNode }) {
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    msalInstance.initialize().then(() => {
      setIsReady(true);
    });
  }, []);

  if (!isReady) {
    return null; 
  }

  return (
    <MsalProvider instance={msalInstance}>
      {children}
    </MsalProvider>
  );
}
