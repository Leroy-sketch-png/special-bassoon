import { Configuration, PublicClientApplication } from "@azure/msal-browser";

export const msalConfig: Configuration = {
  auth: {
    clientId: process.env.NEXT_PUBLIC_AZURE_CLIENT_ID || "",
    authority: `https://login.microsoftonline.com/${process.env.NEXT_PUBLIC_AZURE_TENANT_ID}`,
    redirectUri: typeof window !== "undefined" ? window.location.origin + "/" : "",
  },
  cache: {
    cacheLocation: "sessionStorage"
  }
};

// Add scopes here for ID token to be used at various endpoints.
export const loginRequest = {
  scopes: [`api://${process.env.NEXT_PUBLIC_AZURE_CLIENT_ID}/access_as_user`]
};

export const msalInstance = new PublicClientApplication(msalConfig);
