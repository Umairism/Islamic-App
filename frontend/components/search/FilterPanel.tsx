'use client'

import React, { useState } from 'react'
import { ChevronDown } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { SourceType, HadithAuthenticity } from '@/lib/types'
import { cn } from '@/lib/utils'

interface FilterSection {
  id: string
  title: string
  items: Array<{
    id: string
    label: string
    count: number
  }>
  defaultExpanded?: boolean
}

interface FilterPanelProps {
  sourceTypes?: SourceType[]
  onSourceTypesChange?: (types: SourceType[]) => void
  authenticities?: HadithAuthenticity[]
  onAuthenticityChange?: (auths: HadithAuthenticity[]) => void
  onReset?: () => void
}

/**
 * Collapsible filter panel for advanced search filtering
 * Manages multiple filter categories with counts
 */
export function FilterPanel({
  sourceTypes = [],
  onSourceTypesChange,
  authenticities = [],
  onAuthenticityChange,
  onReset,
}: FilterPanelProps) {
  const [expandedSections, setExpandedSections] = useState<Record<string, boolean>>({
    sourceTypes: true,
    authenticity: true,
  })

  const toggleSection = (sectionId: string) => {
    setExpandedSections((prev) => ({
      ...prev,
      [sectionId]: !prev[sectionId],
    }))
  }

  const sourceTypeOptions: FilterSection = {
    id: 'sourceTypes',
    title: 'Source Type',
    defaultExpanded: true,
    items: [
      { id: SourceType.QURAN, label: 'Quran', count: 114 },
      { id: SourceType.HADITH, label: 'Hadith', count: 3250 },
      { id: SourceType.TAFSIR, label: 'Tafsir', count: 892 },
      { id: SourceType.FIQH, label: 'Islamic Jurisprudence', count: 456 },
    ],
  }

  const authenticityOptions: FilterSection = {
    id: 'authenticity',
    title: 'Authenticity',
    defaultExpanded: true,
    items: [
      { id: HadithAuthenticity.SAHIH, label: 'Sahih (Authentic)', count: 1240 },
      { id: HadithAuthenticity.HASAN, label: 'Hasan (Good)', count: 540 },
      { id: HadithAuthenticity.DAI_IF, label: 'Daiif (Weak)', count: 320 },
    ],
  }

  const FilterCheckbox = ({
    id,
    label,
    count,
    checked,
    onChange,
  }: {
    id: string
    label: string
    count: number
    checked: boolean
    onChange: (checked: boolean) => void
  }) => (
    <label className="flex items-center gap-3 rounded px-2 py-1.5 hover:bg-muted transition-colors cursor-pointer">
      <Checkbox checked={checked} onCheckedChange={onChange} />
      <div className="flex flex-1 items-center justify-between text-sm">
        <span className="text-foreground">{label}</span>
        <span className="text-xs text-muted-foreground">({count.toLocaleString()})</span>
      </div>
    </label>
  )

  const FilterSection = ({
    section,
    onItemChange,
    selectedItems,
  }: {
    section: FilterSection
    onItemChange: (itemId: string, checked: boolean) => void
    selectedItems: string[]
  }) => (
    <div className="border-b border-border last:border-b-0">
      <button
        onClick={() => toggleSection(section.id)}
        className="w-full flex items-center justify-between px-4 py-3 text-sm font-medium text-foreground hover:bg-muted transition-colors"
        aria-expanded={expandedSections[section.id]}
      >
        <span>{section.title}</span>
        <ChevronDown
          className={cn(
            'h-4 w-4 transition-transform',
            expandedSections[section.id] && 'rotate-180'
          )}
        />
      </button>

      {expandedSections[section.id] && (
        <div className="px-4 py-2 space-y-2 bg-background/50">
          {section.items.map((item) => (
            <FilterCheckbox
              key={item.id}
              id={item.id}
              label={item.label}
              count={item.count}
              checked={selectedItems.includes(item.id)}
              onChange={(checked) => onItemChange(item.id, checked)}
            />
          ))}
        </div>
      )}
    </div>
  )

  return (
    <div className="rounded-lg border border-border bg-card overflow-hidden">
      {/* Header */}
      <div className="px-4 py-3 border-b border-border bg-muted/50">
        <h2 className="text-sm font-semibold text-foreground">Filters</h2>
      </div>

      {/* Filter Groups */}
      <div className="divide-y divide-border">
        <FilterSection
          section={sourceTypeOptions}
          onItemChange={(id, checked) => {
            const newTypes = checked
              ? [...sourceTypes, id as SourceType]
              : sourceTypes.filter((t) => t !== id)
            onSourceTypesChange?.(newTypes)
          }}
          selectedItems={sourceTypes}
        />

        <FilterSection
          section={authenticityOptions}
          onItemChange={(id, checked) => {
            const newAuths = checked
              ? [...authenticities, id as HadithAuthenticity]
              : authenticities.filter((a) => a !== id)
            onAuthenticityChange?.(newAuths)
          }}
          selectedItems={authenticities}
        />
      </div>

      {/* Reset Button */}
      {(sourceTypes.length > 0 || authenticities.length > 0) && (
        <div className="px-4 py-3 border-t border-border bg-muted/30">
          <Button
            variant="outline"
            size="sm"
            onClick={onReset}
            className="w-full text-xs"
          >
            Clear Filters
          </Button>
        </div>
      )}
    </div>
  )
}
