'use client';

import React from 'react';
import { ShieldCheck, AlertTriangle } from 'lucide-react';

interface ConfidenceMeterProps {
  score: number; // 0.0 - 1.0
  breakdown?: {
    evidence?: number;
    citation?: number;
    reasoning?: number;
    validation?: number;
  };
}

export const ConfidenceMeter: React.FC<ConfidenceMeterProps> = ({ score, breakdown }) => {
  const percentage = Math.round(score * 100);

  const getScoreColor = (pct: number) => {
    if (pct >= 85) return 'text-emerald-400 bg-emerald-500/20 border-emerald-500/40';
    if (pct >= 70) return 'text-amber-400 bg-amber-500/20 border-amber-500/40';
    return 'text-rose-400 bg-rose-500/20 border-rose-500/40';
  };

  const getBarColor = (pct: number) => {
    if (pct >= 85) return 'bg-emerald-500';
    if (pct >= 70) return 'bg-amber-500';
    return 'bg-rose-500';
  };

  return (
    <div className="p-5 bg-slate-900 border border-slate-800 rounded-xl shadow-xl text-slate-100 mb-6">
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <ShieldCheck className="w-5 h-5 text-emerald-400" />
          <h4 className="font-semibold text-sm text-slate-200">Composite Confidence Score</h4>
        </div>
        <div className={`px-3 py-1 rounded-full border text-sm font-bold ${getScoreColor(percentage)}`}>
          {percentage}% Confidence
        </div>
      </div>

      {/* Progress Bar */}
      <div className="w-full h-3 bg-slate-950 rounded-full overflow-hidden mb-4 border border-slate-800">
        <div
          className={`h-full transition-all duration-700 ease-out ${getBarColor(percentage)}`}
          style={{ width: `${percentage}%` }}
        />
      </div>

      {percentage < 70 && (
        <div className="flex items-center gap-2 p-2.5 bg-amber-950/40 border border-amber-800/50 rounded-lg text-amber-300 text-xs mb-3">
          <AlertTriangle className="w-4 h-4 shrink-0" />
          <span>Lower confidence detected. Exercise scholarly caution with synthesized claims.</span>
        </div>
      )}

      {/* Confidence Breakdown Grid */}
      {breakdown && (
        <div className="grid grid-cols-2 gap-3 pt-3 border-t border-slate-800 text-xs">
          {breakdown.evidence !== undefined && (
            <div className="flex justify-between items-center bg-slate-950/60 p-2 rounded border border-slate-800">
              <span className="text-slate-400">Evidence Depth</span>
              <span className="font-semibold text-emerald-300">{Math.round(breakdown.evidence * 100)}%</span>
            </div>
          )}
          {breakdown.citation !== undefined && (
            <div className="flex justify-between items-center bg-slate-950/60 p-2 rounded border border-slate-800">
              <span className="text-slate-400">Citation Validity</span>
              <span className="font-semibold text-emerald-300">{Math.round(breakdown.citation * 100)}%</span>
            </div>
          )}
          {breakdown.reasoning !== undefined && (
            <div className="flex justify-between items-center bg-slate-950/60 p-2 rounded border border-slate-800">
              <span className="text-slate-400">Reasoning Consistency</span>
              <span className="font-semibold text-emerald-300">{Math.round(breakdown.reasoning * 100)}%</span>
            </div>
          )}
          {breakdown.validation !== undefined && (
            <div className="flex justify-between items-center bg-slate-950/60 p-2 rounded border border-slate-800">
              <span className="text-slate-400">Guard Validation</span>
              <span className="font-semibold text-emerald-300">{Math.round(breakdown.validation * 100)}%</span>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
