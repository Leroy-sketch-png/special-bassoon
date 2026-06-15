import './globals.css';
import { Outfit, Inter } from 'next/font/google';
import { MsalProviderWrapper } from '@/components/MsalProviderWrapper';

const outfit = Outfit({ 
  subsets: ['latin'],
  variable: '--font-outfit',
  display: 'swap',
});

const inter = Inter({ 
  subsets: ['latin'],
  variable: '--font-inter',
  display: 'swap',
});

export const metadata = {
  title: 'MOE e-Service Portal',
  description: 'Ministry of Education e-Service and Administration Portal',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className={`${outfit.variable} ${inter.variable}`}>
      <body className="antialiased">
        <div className="animated-bg">
          <div className="blob blob-1"></div>
          <div className="blob blob-2"></div>
          <div className="blob blob-3"></div>
        </div>
        <div className="glass-overlay"></div>
        <main style={{ padding: '40px', maxWidth: '1200px', margin: '0 auto', position: 'relative', zIndex: 10 }}>
          <MsalProviderWrapper>
            {children}
          </MsalProviderWrapper>
        </main>
      </body>
    </html>
  );
}
