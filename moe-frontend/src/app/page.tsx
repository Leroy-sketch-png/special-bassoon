import Link from 'next/link';

export default function Home() {
  return (
    <div className="flex-center" style={{ flexDirection: 'column', height: '80vh' }}>
      <h1 className="page-title">Welcome to MOE e-Service Portal</h1>
      <p className="page-subtitle" style={{ marginBottom: '32px' }}>A streamlined interface for students and parents.</p>
      
      <div className="grid-cols-2" style={{ gap: '16px' }}>
        <Link href="/login" className="btn-primary" style={{ textAlign: 'center', textDecoration: 'none' }}>
          Login with Singpass
        </Link>
      </div>
    </div>
  );
}
