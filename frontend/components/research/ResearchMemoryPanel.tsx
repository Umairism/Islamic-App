'use client';

import React from 'react';
import { MemoryEntryDto } from '@/lib/types';
import { Database, Clock, FileText } from 'lucide-react';

interface ResearchMemoryPanelProps {
  memories: MemoryEntryDto[];
  onSelectMemory?: (memory: MemoryEntryDto) => void;
}

export const ResearchMemoryPanel: React.FC<ResearchMemoryPanelProps> = ({
  memories,
  onSelectMemory,
}) => {
  return (
    <div className="p-5 bg-slate-900 border border-slate-800 rounded-xl shadow-xl text-slate-100 mb-6">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Database className="w-5 h-5 text-emerald-400" />
          <h4 className="font-semibold text-sm text-slate-200">Workspace Research Memories</h4>
        </div>
        <span className="px-2.5 py-0.5 bg-slate-800 text-slate-300 text-xs font-mono rounded-full">
          {memories.length} Stored
        </span>
      </div>

      {memories.length === 0 ? (
        <div className="p-6 text-center border border-dashed border-slate-800 rounded-lg text-slate-500 text-xs">
          No research memories recorded for this workspace yet. Execute a session to persist memories.
        </div>
      ) : (
        <div className="space-y-3 max-h-80 overflow-y-auto pr-1">
          {memories.map((mem) => (
            <div
              key={mem.id}
              onClick={() => onSelectMemory?.(mem)}
              className="p-3 bg-slate-950/60 border border-slate-800 rounded-lg hover:border-emerald-500/50 hover:bg-slate-950 transition-all cursor-pointer group"
            >
              <div className="flex items-center justify-between mb-1.5">
                <span className="font-medium text-xs text-emerald-400 group-hover:text-emerald-300 flex items-center gap-1.5">
                  <FileText className="w-3.5 h-3.5" />
                  {mem.query}
                </span>
                <span className="text-[10px] text-slate-400 flex items-center gap-1">
                  <Clock className="w-3 h-3" />
                  {new Date(mem.createdAt).toLocaleDateString()}
                </span>
              </div>
              <p className="text-xs text-slate-300 line-clamp-2 mb-2 leading-relaxed">{mem.summary}</p>
              <div className="flex items-center justify-between text-[11px] text-slate-400 border-t border-slate-800/80 pt-1.5">
                <span>Confidence: <strong className="text-slate-200">{Math.round(mem.confidenceOverall * 100)}%</strong></span>
                <span>Evidences: <strong className="text-slate-200">{mem.evidenceCount}</strong></span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
