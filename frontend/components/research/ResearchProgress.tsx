'use client';

import React from 'react';
import { PipelineStage, ResearchSessionStatus } from '@/lib/types';
import { CheckCircle2, Circle, Loader2, AlertCircle } from 'lucide-react';

interface ResearchProgressProps {
  status: ResearchSessionStatus;
  currentStage: PipelineStage | null;
  completedStages: PipelineStage[];
  errorMessage?: string;
}

const ALL_STAGES: { stage: PipelineStage; label: string; description: string }[] = [
  { stage: PipelineStage.Retrieval, label: 'Retrieval', description: 'Querying Quran & Hadith dataset' },
  { stage: PipelineStage.Deduplication, label: 'Deduplication', description: 'Filter redundant evidence nodes' },
  { stage: PipelineStage.Analysis, label: 'Graph Analysis', description: 'Analyze thematic relationships & conflicts' },
  { stage: PipelineStage.Reasoning, label: 'Synthesis Reasoning', description: 'Synthesize Islamic jurisprudence claims' },
  { stage: PipelineStage.Validation, label: 'Publishability Validation', description: 'Verify citations against hallucination guards' },
  { stage: PipelineStage.Explainability, label: 'Explainability Trace', description: 'Map claim provenance & evidence lineage' },
  { stage: PipelineStage.Rendering, label: 'Rendering', description: 'Formatting Markdown & HTML dossiers' },
];

export const ResearchProgress: React.FC<ResearchProgressProps> = ({
  status,
  currentStage,
  completedStages,
  errorMessage,
}) => {
  return (
    <div className="p-6 bg-slate-900 border border-slate-800 rounded-xl shadow-xl text-slate-100 mb-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-emerald-400 flex items-center gap-2">
          <span>Research Execution Progress</span>
        </h3>
        <span
          className={`px-3 py-1 text-xs font-semibold rounded-full uppercase tracking-wider ${
            status === ResearchSessionStatus.Completed
              ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
              : status === ResearchSessionStatus.Failed
              ? 'bg-rose-500/20 text-rose-300 border border-rose-500/40'
              : 'bg-amber-500/20 text-amber-300 border border-amber-500/40 animate-pulse'
          }`}
        >
          {status}
        </span>
      </div>

      {errorMessage && (
        <div className="mb-4 p-3 bg-rose-950/60 border border-rose-800/80 rounded-lg text-rose-300 text-sm flex items-center gap-2">
          <AlertCircle className="w-5 h-5 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      <div className="space-y-3">
        {ALL_STAGES.map((s, index) => {
          const isCompleted = completedStages.includes(s.stage) || status === ResearchSessionStatus.Completed;
          const isCurrent = currentStage === s.stage && status === ResearchSessionStatus.Running;

          return (
            <div
              key={s.stage}
              className={`flex items-start gap-3 p-3 rounded-lg border transition-all ${
                isCompleted
                  ? 'bg-emerald-950/20 border-emerald-800/40 text-emerald-200'
                  : isCurrent
                  ? 'bg-amber-950/30 border-amber-600/60 text-amber-200 ring-1 ring-amber-500/50'
                  : 'bg-slate-950/40 border-slate-800/60 text-slate-400 opacity-60'
              }`}
            >
              <div className="mt-0.5">
                {isCompleted ? (
                  <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
                ) : isCurrent ? (
                  <Loader2 className="w-5 h-5 text-amber-400 animate-spin shrink-0" />
                ) : (
                  <Circle className="w-5 h-5 text-slate-600 shrink-0" />
                )}
              </div>
              <div className="flex-1">
                <div className="flex items-center justify-between">
                  <span className="font-medium text-sm">
                    {index + 1}. {s.label}
                  </span>
                  {isCurrent && <span className="text-xs text-amber-400 font-mono">Processing...</span>}
                  {isCompleted && <span className="text-xs text-emerald-400 font-mono">Done</span>}
                </div>
                <p className="text-xs text-slate-400 mt-0.5">{s.description}</p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
