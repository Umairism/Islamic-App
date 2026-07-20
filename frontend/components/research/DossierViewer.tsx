'use client';

import React, { useState } from 'react';
import { FileText, Download, Copy, Check } from 'lucide-react';

interface DossierViewerProps {
  markdownContent?: string;
  contentHash?: string;
  sessionId?: string;
}

export const DossierViewer: React.FC<DossierViewerProps> = ({
  markdownContent,
  contentHash,
  sessionId,
}) => {
  const [copied, setCopied] = useState(false);

  const copyToClipboard = () => {
    if (!markdownContent) return;
    navigator.clipboard.writeText(markdownContent);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const downloadFile = () => {
    if (!markdownContent) return;
    const blob = new Blob([markdownContent], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `dossier-${sessionId?.substring(0, 8) || 'export'}.md`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  if (!markdownContent) {
    return (
      <div className="p-6 bg-slate-900 border border-slate-800 rounded-xl shadow-xl text-slate-100">
        <div className="flex items-center gap-2 mb-2">
          <FileText className="w-5 h-5 text-emerald-400" />
          <h3 className="font-semibold text-base text-slate-200">Legal-Grade Research Dossier</h3>
        </div>
        <p className="text-xs text-slate-500 italic">
          Dossier compilation occurs upon pipeline completion...
        </p>
      </div>
    );
  }

  return (
    <div className="p-6 bg-slate-900 border border-slate-800 rounded-xl shadow-xl text-slate-100 space-y-4">
      <div className="flex items-center justify-between border-b border-slate-800 pb-4">
        <div className="flex items-center gap-2">
          <FileText className="w-5 h-5 text-emerald-400" />
          <h3 className="font-semibold text-base text-slate-200">Legal-Grade Research Dossier</h3>
        </div>
        <div className="flex items-center gap-2">
          {contentHash && (
            <span className="px-2.5 py-1 bg-slate-950 text-slate-400 text-[10px] font-mono rounded border border-slate-800" title={`SHA-256: ${contentHash}`}>
              SHA: {contentHash.substring(0, 8)}...
            </span>
          )}
          <button
            onClick={copyToClipboard}
            className="p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-xs flex items-center gap-1.5 transition-all"
          >
            {copied ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
            <span>{copied ? 'Copied' : 'Copy'}</span>
          </button>
          <button
            onClick={downloadFile}
            className="px-3 py-2 bg-emerald-600 hover:bg-emerald-500 text-white font-semibold rounded-lg text-xs flex items-center gap-1.5 shadow-md shadow-emerald-950/40 transition-all"
          >
            <Download className="w-4 h-4" />
            <span>Export Markdown</span>
          </button>
        </div>
      </div>

      <div className="p-5 bg-slate-950/80 border border-slate-800 rounded-lg max-h-96 overflow-y-auto font-mono text-xs text-slate-300 leading-relaxed whitespace-pre-wrap">
        {markdownContent}
      </div>
    </div>
  );
};
