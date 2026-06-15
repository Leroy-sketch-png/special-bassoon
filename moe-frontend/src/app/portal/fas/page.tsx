"use client";

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { api, AiChatMessage, FasDraftFields, CitizenRecord } from '@/lib/apiClient';

export default function FASPage() {
  const router = useRouter();
  const [messages, setMessages] = useState<AiChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  
  const [draft, setDraft] = useState<FasDraftFields>({});
  const [citizenProfile, setCitizenProfile] = useState<CitizenRecord | null>(null);
  
  const [consentGiven, setConsentGiven] = useState(false);
  const [declarationTrue, setDeclarationTrue] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    // Load citizen profile to pre-fill trusted fields like Nationality
    const token = document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1];
    if (!token) {
      router.push('/login');
      return;
    }

    api.eligibility.me(token).then(profile => {
      setCitizenProfile(profile);
    }).catch(e => {
      console.error("Failed to load citizen profile", e);
    });
  }, [router]);

  const sendMessage = async () => {
    if (!input.trim()) return;
    
    const userMsg: AiChatMessage = { role: 'user', content: input };
    const newMessages = [...messages, userMsg];
    setMessages(newMessages);
    setInput('');
    setLoading(true);

    try {
      const token = document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1] || '';
      
      const citizenContext = {
        nationality: "SINGAPORE CITIZEN", // Mocked for now, in real life from Myinfo/Singpass
        student_id: citizenProfile?.nric
      };

      const data = await api.ai.chat(
        input, 
        'fas-draft', 
        messages, 
        token, 
        JSON.stringify(citizenContext)
      );
      
      if (data.updatedDraftFields) {
        setDraft(prev => ({ ...prev, ...data.updatedDraftFields }));
      }

      setMessages([...newMessages, { role: 'assistant', content: data.reply }]);
    } catch (e) {
      console.error(e);
      setMessages([...newMessages, { role: 'assistant', content: 'Sorry, I encountered an error processing your draft.' }]);
    }
    setLoading(false);
  };

  const submitFAS = async () => {
    try {
      setLoading(true);
      const token = document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1] || '';
      await api.fas.submit(JSON.stringify(draft), token);
      setSubmitted(true);
    } catch (e: any) {
      console.error(e);
      alert(e.message || "Failed to submit application");
    } finally {
      setLoading(false);
    }
  };

  if (submitted) {
    return (
      <div className="flex-center" style={{ minHeight: '60vh' }}>
        <div className="glass-panel" style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '4rem', marginBottom: '16px' }}>🎉</div>
          <h2>Application Submitted</h2>
          <p style={{ color: 'var(--text-secondary)' }}>Your FAS application has been successfully submitted for review.</p>
        </div>
      </div>
    );
  }

  // Determine if form is ready to submit (all fields collected + checkboxes ticked)
  const isFormComplete = 
    draft.household_income !== undefined &&
    draft.household_size !== undefined &&
    draft.num_dependants !== undefined &&
    draft.school_name !== undefined &&
    draft.year_of_study !== undefined &&
    draft.reason_for_application !== undefined;

  const canSubmit = isFormComplete && consentGiven && declarationTrue;

  return (
    <div>
      <div style={{ marginBottom: '32px' }}>
        <h1 className="page-title">FAS Application</h1>
        <p className="page-subtitle">Let our AI assistant guide you through the Financial Assistance Scheme application.</p>
      </div>

      <div className="grid-cols-2" style={{ alignItems: 'flex-start' }}>
        
        {/* Chat Panel */}
        <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', height: '700px', padding: 0 }}>
          <div style={{ padding: '16px', borderBottom: '1px solid var(--glass-border)', background: 'rgba(0,0,0,0.02)' }}>
            <h3 style={{ margin: 0, fontSize: '1.1rem' }}>AI Drafter</h3>
          </div>
          <div style={{ flex: 1, overflowY: 'auto', padding: '16px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
            {messages.length === 0 && (
              <div style={{ color: 'var(--text-secondary)' }}>
                Hi! I can help you fill out the FAS application. Could you tell me your monthly gross household income to start?
              </div>
            )}
            {messages.map((msg, i) => (
              <div key={i} style={{ 
                alignSelf: msg.role === 'user' ? 'flex-end' : 'flex-start',
                maxWidth: '85%',
                padding: '10px 14px',
                borderRadius: '12px',
                background: msg.role === 'user' ? 'var(--primary-color)' : 'var(--glass-bg)',
                color: msg.role === 'user' ? '#fff' : 'inherit',
                border: msg.role === 'assistant' ? '1px solid var(--glass-border)' : 'none',
              }}>
                {msg.content}
              </div>
            ))}
            {loading && <div style={{ color: 'var(--text-secondary)', fontStyle: 'italic' }}>Thinking...</div>}
          </div>
          <div style={{ padding: '16px', borderTop: '1px solid var(--glass-border)' }}>
             <div className="flex-between" style={{ gap: '8px' }}>
              <input 
                type="text" 
                className="input-field" 
                value={input} 
                onChange={e => setInput(e.target.value)} 
                onKeyDown={e => e.key === 'Enter' && sendMessage()}
                placeholder="Type here..."
              />
              <button className="btn-primary" onClick={sendMessage} disabled={loading || !input.trim()}>Send</button>
            </div>
          </div>
        </div>

        {/* Live Form Preview */}
        <div className="glass-panel" style={{ height: '700px', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '24px' }}>
          <div>
            <h3 style={{ marginBottom: '16px' }}>Application Draft</h3>
            
            {/* Section 1: Trusted Prefill */}
            <div style={{ padding: '16px', background: 'rgba(0,0,0,0.02)', borderRadius: '8px', border: '1px solid var(--glass-border)' }}>
              <h4 style={{ margin: '0 0 12px 0', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                🔒 Government-verified data
              </h4>
              <div className="grid-cols-2" style={{ gap: '12px' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.8rem', color: '#666', marginBottom: '2px' }}>Nationality</label>
                  <div style={{ fontWeight: 500 }}>SINGAPORE CITIZEN</div>
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '0.8rem', color: '#666', marginBottom: '2px' }}>Student ID</label>
                  <div style={{ fontWeight: 500 }}>{citizenProfile?.nric || '...'}</div>
                </div>
              </div>
            </div>
          </div>

          {/* Section 2: AI-filled fields */}
          <div>
            <h4 style={{ margin: '0 0 12px 0', fontSize: '0.9rem', color: 'var(--accent-color)' }}>
              ✨ AI-filled fields — please review
            </h4>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              
              <div className="grid-cols-2" style={{ gap: '12px' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Household Income (SGD)</label>
                  <div style={{ padding: '8px 12px', background: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--accent-color)' }}>
                    {draft.household_income !== undefined ? `$${draft.household_income}` : <span style={{ color: '#999', fontStyle: 'italic' }}>Pending...</span>}
                  </div>
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Household Size</label>
                  <div style={{ padding: '8px 12px', background: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--accent-color)' }}>
                    {draft.household_size !== undefined ? draft.household_size : <span style={{ color: '#999', fontStyle: 'italic' }}>Pending...</span>}
                  </div>
                </div>
              </div>

              <div className="grid-cols-2" style={{ gap: '12px' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Number of Dependants</label>
                  <div style={{ padding: '8px 12px', background: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--accent-color)' }}>
                    {draft.num_dependants !== undefined ? draft.num_dependants : <span style={{ color: '#999', fontStyle: 'italic' }}>Pending...</span>}
                  </div>
                </div>
                <div>
                  <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Year of Study</label>
                  <div style={{ padding: '8px 12px', background: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--accent-color)' }}>
                    {draft.year_of_study || <span style={{ color: '#999', fontStyle: 'italic' }}>Pending...</span>}
                  </div>
                </div>
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>School Name</label>
                <div style={{ padding: '8px 12px', background: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--accent-color)' }}>
                  {draft.school_name || <span style={{ color: '#999', fontStyle: 'italic' }}>Pending...</span>}
                </div>
              </div>

              <div>
                <label style={{ display: 'block', fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '4px' }}>Reason for Application</label>
                <div style={{ padding: '8px 12px', background: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--accent-color)', minHeight: '60px' }}>
                  {draft.reason_for_application || <span style={{ color: '#999', fontStyle: 'italic' }}>Pending...</span>}
                </div>
              </div>

            </div>
          </div>

          {/* Section 3: Declarations */}
          {isFormComplete && (
            <div className="fade-in" style={{ marginTop: 'auto' }}>
              <h4 style={{ margin: '0 0 12px 0', fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                📝 Your declarations
              </h4>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginBottom: '24px' }}>
                <label style={{ display: 'flex', alignItems: 'flex-start', gap: '12px', cursor: 'pointer' }}>
                  <input 
                    type="checkbox" 
                    checked={consentGiven}
                    onChange={e => setConsentGiven(e.target.checked)}
                    style={{ width: '18px', height: '18px', marginTop: '2px' }} 
                  />
                  <span style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                    I consent to MOE retrieving my data from Myinfo for the purpose of this application.
                  </span>
                </label>
                
                <label style={{ display: 'flex', alignItems: 'flex-start', gap: '12px', cursor: 'pointer' }}>
                  <input 
                    type="checkbox" 
                    checked={declarationTrue}
                    onChange={e => setDeclarationTrue(e.target.checked)}
                    style={{ width: '18px', height: '18px', marginTop: '2px' }} 
                  />
                  <span style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>
                    I declare that all information provided above is true and correct to the best of my knowledge. I understand that false declarations may result in disqualification.
                  </span>
                </label>
              </div>

              <button 
                className="btn-primary" 
                style={{ width: '100%', padding: '14px', opacity: canSubmit ? 1 : 0.5 }} 
                disabled={!canSubmit}
                onClick={submitFAS}
              >
                Submit Application
              </button>
            </div>
          )}
        </div>

      </div>
    </div>
  );
}
