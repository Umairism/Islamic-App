'use client';

import React, { useState, useEffect } from 'react';
import {
  PipelineStage,
  ResearchClaim,
  ResearchSessionStatus,
  MemoryEntryDto,
  EvaluationResultDto,
} from '@/lib/types';
import { signalRClient } from '@/lib/signalRClient';
import { ResearchProgress } from './ResearchProgress';
import { ConfidenceMeter } from './ConfidenceMeter';
import { ResearchMemoryPanel } from './ResearchMemoryPanel';
import { EvaluationReport } from './EvaluationReport';
import { DossierViewer } from './DossierViewer';
import { Search, Sparkles, BookOpen, Layers, CheckCircle } from 'lucide-react';

export const ResearchWorkspace: React.FC = () => {
  const [query, setQuery] = useState('What are the ruling about circumcision in Islam?');
  const [workspaceId] = useState('00000000-0000-0000-0000-000000000001');
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [status, setStatus] = useState<ResearchSessionStatus>(ResearchSessionStatus.Created);
  const [currentStage, setCurrentStage] = useState<PipelineStage | null>(null);
  const [completedStages, setCompletedStages] = useState<PipelineStage[]>([]);
  const [summary, setSummary] = useState<string>('');
  const [claims, setClaims] = useState<ResearchClaim[]>([]);
  const [confidence, setConfidence] = useState<number>(0.95);
  const [memories, setMemories] = useState<MemoryEntryDto[]>([]);
  const [evaluation, setEvaluation] = useState<EvaluationResultDto | null>(null);
  const [dossierMarkdown, setDossierMarkdown] = useState<string>('');
  const [dossierHash, setDossierHash] = useState<string>('');
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  useEffect(() => {
    fetchMemories();
  }, []);

  const fetchMemories = async () => {
    try {
      const res = await fetch(`http://localhost:5000/api/research/workspaces/${workspaceId}/memories`);
      if (res.ok) {
        const data = await res.json();
        setMemories(data);
      }
    } catch {
      // Backend api offline fallback
    }
  };

  const startResearchSession = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!query.trim() || isSubmitting) return;

    setIsSubmitting(true);
    setErrorMessage('');
    setSummary('');
    setClaims([]);
    setEvaluation(null);
    setDossierMarkdown('');
    setCompletedStages([]);
    setStatus(ResearchSessionStatus.Queued);

    try {
      // 1. Post new session request to backend API
      const res = await fetch('http://localhost:5000/api/research/sessions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ query, workspaceId }),
      });

      if (!res.ok) {
        throw new Error(`Failed to initialize session (${res.status})`);
      }

      const data = await res.json();
      const newSessionId = data.id;
      setSessionId(newSessionId);
      setStatus(ResearchSessionStatus.Running);

      // 2. Connect SignalR Hub & Join Session Group
      await signalRClient.connect();
      await signalRClient.joinSessionGroup(newSessionId);

      signalRClient.onStageCompleted((stage: PipelineStage) => {
        setCompletedStages((prev) => [...prev, stage]);
        setCurrentStage(stage);
      });

      signalRClient.onSessionStatusChanged((_, newStatus: ResearchSessionStatus) => {
        setStatus(newStatus);
        if (newStatus === ResearchSessionStatus.Completed) {
          fetchFinalResult(newSessionId);
          fetchEvaluation(newSessionId);
          fetchDossier(newSessionId);
          fetchMemories();
        }
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error executing research pipeline';
      setErrorMessage(msg);
      setStatus(ResearchSessionStatus.Failed);
    } finally {
      setIsSubmitting(false);
    }
  };

  const fetchFinalResult = async (sid: string) => {
    try {
      const res = await fetch(`http://localhost:5000/api/research/sessions/${sid}/result`);
      if (res.ok) {
        const data = await res.json();
        setSummary(data.answerText || data.summary || '');
        setClaims(data.claims || []);
        if (data.confidenceScore) setConfidence(data.confidenceScore);
      }
    } catch {
      // API fallback
    }
  };

  const fetchEvaluation = async (sid: string) => {
    try {
      const res = await fetch(`http://localhost:5000/api/v1/research/${sid}/evaluation`);
      if (res.ok) {
        const data = await res.json();
        setEvaluation(data);
      }
    } catch {
      // API fallback
    }
  };

  const fetchDossier = async (sid: string) => {
    try {
      const res = await fetch(`http://localhost:5000/api/v1/dossiers/${sid}`);
      if (res.ok) {
        const data = await res.json();
        setDossierMarkdown(data.markdownContent || '');
        setDossierHash(data.contentHash || '');
      }
    } catch {
      // API fallback
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-6 md:p-12 font-sans">
      <div className="max-w-6xl mx-auto space-y-8">
        {/* Header */}
        <div className="border-b border-slate-800 pb-6">
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2.5 bg-emerald-500/10 border border-emerald-500/30 rounded-xl text-emerald-400">
              <Sparkles className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-2xl md:text-3xl font-bold bg-gradient-to-r from-emerald-400 via-teal-300 to-cyan-400 bg-clip-text text-transparent">
                Islamic AI Research Platform
              </h1>
              <p className="text-sm text-slate-400 mt-1">
                Autonomous Research Verification, Evaluation & Dossier Generation System
              </p>
            </div>
          </div>
        </div>

        {/* Input Form */}
        <form onSubmit={startResearchSession} className="relative">
          <div className="relative flex items-center">
            <Search className="absolute left-4 w-5 h-5 text-slate-400" />
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Ask an Islamic jurisprudence question (e.g. Khitan, Taharah, Zakat)..."
              className="w-full pl-12 pr-36 py-4 bg-slate-900 border border-slate-800 rounded-xl text-slate-100 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/50 focus:border-emerald-500/50 text-sm md:text-base shadow-2xl transition-all"
            />
            <button
              type="submit"
              disabled={isSubmitting}
              className="absolute right-2.5 px-5 py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-semibold rounded-lg shadow-lg shadow-emerald-950/40 transition-all flex items-center gap-2 disabled:opacity-50"
            >
              {isSubmitting ? (
                <><span>Queueing...</span></>
              ) : (
                <>
                  <BookOpen className="w-4 h-4" />
                  <span>Execute Research</span>
                </>
              )}
            </button>
          </div>
        </form>

        {/* Main Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Left Column: Progress, Memory & Evaluation */}
          <div className="lg:col-span-1 space-y-6">
            <ResearchProgress
              status={status}
              currentStage={currentStage}
              completedStages={completedStages}
              errorMessage={errorMessage}
            />
            <ConfidenceMeter score={confidence} />
            <EvaluationReport evaluation={evaluation} />
            <ResearchMemoryPanel memories={memories} />
          </div>

          {/* Right Column: Reasoning Output, Claims & Dossier Viewer */}
          <div className="lg:col-span-2 space-y-6">
            <div className="p-6 bg-slate-900 border border-slate-800 rounded-xl shadow-xl space-y-6">
              <div className="flex items-center justify-between border-b border-slate-800 pb-4">
                <h3 className="text-lg font-semibold text-slate-200 flex items-center gap-2">
                  <Layers className="w-5 h-5 text-emerald-400" />
                  <span>Synthesized Research Dossier</span>
                </h3>
                {sessionId && (
                  <span className="text-xs font-mono text-slate-500">Session ID: {sessionId.substring(0, 8)}...</span>
                )}
              </div>

              {/* Summary Dossier */}
              <div>
                <h4 className="text-xs uppercase tracking-wider font-semibold text-emerald-400 mb-2">
                  Jurisprudential Synthesis Summary
                </h4>
                <div className="p-4 bg-slate-950/70 border border-slate-800/80 rounded-lg text-slate-200 text-sm leading-relaxed">
                  {summary ? (
                    <p>{summary}</p>
                  ) : (
                    <p className="text-slate-500 italic">
                      Execute a query to stream the live research dossier and claims synthesis...
                    </p>
                  )}
                </div>
              </div>

              {/* Synthesized Claims */}
              <div>
                <h4 className="text-xs uppercase tracking-wider font-semibold text-emerald-400 mb-3">
                  Verified Claims & Evidence Lineage
                </h4>
                {claims.length === 0 ? (
                  <div className="p-4 bg-slate-950/40 border border-slate-800/60 rounded-lg text-slate-500 text-xs italic">
                    Claims will populate dynamically as reasoning validation completes.
                  </div>
                ) : (
                  <div className="space-y-3">
                    {claims.map((c, i) => (
                      <div
                        key={i}
                        className="p-4 bg-slate-950/80 border border-slate-800 rounded-lg space-y-2 hover:border-emerald-500/30 transition-all"
                      >
                        <div className="flex items-start justify-between gap-4">
                          <p className="text-sm text-slate-100 font-medium leading-snug flex items-start gap-2">
                            <CheckCircle className="w-4 h-4 text-emerald-400 shrink-0 mt-0.5" />
                            <span>{c.statement}</span>
                          </p>
                          <span className="px-2 py-0.5 bg-emerald-500/20 text-emerald-300 text-xs font-bold rounded-md shrink-0">
                            {Math.round(c.confidence * 100)}%
                          </span>
                        </div>
                        {c.supportingEvidence && c.supportingEvidence.length > 0 && (
                          <div className="pt-2 border-t border-slate-800/60 flex items-center gap-2 text-xs text-slate-400">
                            <span className="font-semibold text-slate-500">Citations:</span>
                            {c.supportingEvidence.map((ev, idx) => (
                              <span
                                key={idx}
                                className="px-2 py-0.5 bg-slate-900 border border-slate-800 rounded text-emerald-400 font-mono text-[11px]"
                              >
                                {ev}
                              </span>
                            ))}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Dossier Viewer */}
            <DossierViewer
              markdownContent={dossierMarkdown}
              contentHash={dossierHash}
              sessionId={sessionId || undefined}
            />
          </div>
        </div>
      </div>
    </div>
  );
};

