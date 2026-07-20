'use client';

import React from 'react';
import { EvaluationResultDto } from '@/lib/types';
import { BarChart3, AlertTriangle, ShieldCheck, Info } from 'lucide-react';

interface EvaluationReportProps {
  evaluation: EvaluationResultDto | null;
}

export const EvaluationReport: React.FC<EvaluationReportProps> = ({ evaluation }) => {
  if (!evaluation) {
    return (
      <div className="p-5 bg-slate-900 border border-slate-800 rounded-xl shadow-xl text-slate-100 mb-6">
        <div className="flex items-center gap-2 mb-2">
          <BarChart3 className="w-5 h-5 text-emerald-400" />
          <h4 className="font-semibold text-sm text-slate-200">Research Quality Evaluation</h4>
        </div>
        <p className="text-xs text-slate-500 italic">
          Quality evaluation report will populate automatically upon pipeline execution...
        </p>
      </div>
    );
  }

  const renderMetric = (label: string, value: number) => {
    const pct = Math.round(value * 100);
    return (
      <div className="space-y-1">
        <div className="flex justify-between items-center text-xs">
          <span className="text-slate-300 font-medium">{label}</span>
          <span className="font-mono text-emerald-400 font-semibold">{pct}%</span>
        </div>
        <div className="w-full h-2 bg-slate-950 rounded-full overflow-hidden border border-slate-800">
          <div
            className="h-full bg-emerald-500 transition-all duration-500"
            style={{ width: `${pct}%` }}
          />
        </div>
      </div>
    );
  };

  return (
    <div className="p-5 bg-slate-900 border border-slate-800 rounded-xl shadow-xl text-slate-100 mb-6 space-y-4">
      <div className="flex items-center justify-between border-b border-slate-800 pb-3">
        <div className="flex items-center gap-2">
          <BarChart3 className="w-5 h-5 text-emerald-400" />
          <h4 className="font-semibold text-sm text-slate-200">Research Quality Evaluation</h4>
        </div>
        <div className="flex items-center gap-2">
          <span className="px-2.5 py-0.5 bg-emerald-500/20 text-emerald-300 text-xs font-bold border border-emerald-500/40 rounded-full">
            {Math.round(evaluation.overallScore * 100)}% Overall
          </span>
          <span className="text-[10px] text-slate-500 font-mono">v{evaluation.evaluationVersion}</span>
        </div>
      </div>

      {/* Metric Bars */}
      <div className="space-y-3">
        {renderMetric('Evidence Coverage', evaluation.evidenceCoverage)}
        {renderMetric('Citation Accuracy', evaluation.citationAccuracy)}
        {renderMetric('Reasoning Consistency', evaluation.reasoningConsistency)}
        {renderMetric('Source Diversity', evaluation.sourceDiversity)}
      </div>

      {/* Findings Section */}
      {evaluation.findings && evaluation.findings.length > 0 && (
        <div className="pt-3 border-t border-slate-800 space-y-2">
          <h5 className="text-xs uppercase tracking-wider font-semibold text-slate-400">
            Evaluation Findings & Warnings
          </h5>
          <div className="space-y-2">
            {evaluation.findings.map((f, i) => (
              <div
                key={i}
                className={`p-2.5 rounded-lg border text-xs flex items-start gap-2 ${
                  f.severity === 'Error'
                    ? 'bg-rose-950/40 border-rose-800/60 text-rose-300'
                    : f.severity === 'Warning'
                    ? 'bg-amber-950/40 border-amber-800/60 text-amber-300'
                    : 'bg-slate-950/60 border-slate-800 text-slate-300'
                }`}
              >
                {f.severity === 'Error' ? (
                  <AlertTriangle className="w-4 h-4 shrink-0 text-rose-400 mt-0.5" />
                ) : f.severity === 'Warning' ? (
                  <AlertTriangle className="w-4 h-4 shrink-0 text-amber-400 mt-0.5" />
                ) : (
                  <Info className="w-4 h-4 shrink-0 text-slate-400 mt-0.5" />
                )}
                <div>
                  <span className="font-semibold uppercase tracking-wider text-[10px] mr-1 text-slate-400">
                    [{f.category}]
                  </span>
                  <span>{f.description}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
