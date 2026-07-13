'use client'

import React, { useState } from 'react'
import Link from 'next/link'
import {
  Plus,
  Trash2,
  Edit2,
  Eye,
  EyeOff,
  Share2,
  Download,
} from 'lucide-react'
import { useCollections } from '@/hooks/useCollections'
import { useSearch } from '@/hooks/useSearch'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'

/**
 * Collections page client component
 * Displays all user collections with ability to create, edit, and delete
 */
export function CollectionsPageClient() {
  const collections = useCollections()
  const search = useSearch()
  const [newCollectionName, setNewCollectionName] = useState('')
  const [showNewForm, setShowNewForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState('')

  const handleCreateCollection = () => {
    if (newCollectionName.trim()) {
      collections.createCollection(newCollectionName)
      setNewCollectionName('')
      setShowNewForm(false)
    }
  }

  const handleUpdateCollection = (id: string) => {
    if (editingName.trim()) {
      collections.updateCollection(id, { name: editingName })
      setEditingId(null)
      setEditingName('')
    }
  }

  const startEdit = (id: string, currentName: string) => {
    setEditingId(id)
    setEditingName(currentName)
  }

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-foreground">
            My Collections
          </h1>
          <p className="text-muted-foreground mt-2">
            Organize and manage your saved research documents
          </p>
        </div>

        <Button onClick={() => setShowNewForm(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          New Collection
        </Button>
      </div>

      {/* New Collection Form */}
      {showNewForm && (
        <div className="mb-8 rounded-lg border border-border bg-card p-4">
          <h2 className="font-semibold text-foreground mb-4">
            Create New Collection
          </h2>
          <div className="flex gap-2">
            <Input
              placeholder="Collection name..."
              value={newCollectionName}
              onChange={(e) => setNewCollectionName(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  handleCreateCollection()
                }
              }}
              className="flex-1"
              autoFocus
            />
            <Button onClick={handleCreateCollection}>Create</Button>
            <Button
              variant="outline"
              onClick={() => {
                setShowNewForm(false)
                setNewCollectionName('')
              }}
            >
              Cancel
            </Button>
          </div>
        </div>
      )}

      {/* Collections Grid */}
      {collections.collections.length === 0 ? (
        <div className="rounded-lg border border-dashed border-border p-12 text-center">
          <p className="text-lg font-medium text-foreground mb-2">
            No collections yet
          </p>
          <p className="text-muted-foreground mb-6">
            Create your first collection to start organizing your research
          </p>
          <Button onClick={() => setShowNewForm(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            Create Collection
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {collections.collections.map((collection) => (
            <div
              key={collection.id}
              className="rounded-lg border border-border bg-card p-6 hover:shadow-md transition-shadow"
            >
              {/* Header */}
              {editingId === collection.id ? (
                <div className="mb-4 flex gap-2">
                  <Input
                    value={editingName}
                    onChange={(e) => setEditingName(e.target.value)}
                    className="flex-1 text-base font-semibold"
                    autoFocus
                  />
                  <Button
                    size="sm"
                    onClick={() => handleUpdateCollection(collection.id)}
                  >
                    Save
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setEditingId(null)}
                  >
                    Cancel
                  </Button>
                </div>
              ) : (
                <div className="mb-4">
                  <h3 className="text-lg font-semibold text-foreground line-clamp-2">
                    {collection.name}
                  </h3>
                  {collection.description && (
                    <p className="text-sm text-muted-foreground mt-1 line-clamp-2">
                      {collection.description}
                    </p>
                  )}
                </div>
              )}

              {/* Stats */}
              <div className="mb-4 p-3 rounded-md bg-muted/50">
                <p className="text-sm text-muted-foreground">
                  <span className="font-semibold text-foreground">
                    {collection.documentIds.length}
                  </span>
                  {' '}
                  document{collection.documentIds.length !== 1 ? 's' : ''}
                </p>
              </div>

              {/* Tags */}
              {collection.tags && collection.tags.length > 0 && (
                <div className="mb-4 flex flex-wrap gap-1">
                  {collection.tags.slice(0, 2).map((tag) => (
                    <Badge key={tag} variant="secondary" className="text-xs">
                      {tag}
                    </Badge>
                  ))}
                  {collection.tags.length > 2 && (
                    <Badge variant="secondary" className="text-xs">
                      +{collection.tags.length - 2}
                    </Badge>
                  )}
                </div>
              )}

              {/* Actions */}
              <div className="flex flex-wrap gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  asChild
                  className="flex-1 gap-1"
                >
                  <Link href={`/collections/${collection.id}`}>
                    <Eye className="h-3.5 w-3.5" />
                    View
                  </Link>
                </Button>

                <Button
                  variant="outline"
                  size="sm"
                  onClick={() =>
                    startEdit(collection.id, collection.name)
                  }
                  className="gap-1"
                >
                  <Edit2 className="h-3.5 w-3.5" />
                </Button>

                <Button
                  variant="outline"
                  size="sm"
                  onClick={() =>
                    collection.isPublic
                      ? collections.updateCollection(collection.id, {
                          isPublic: false,
                        })
                      : collections.updateCollection(collection.id, {
                          isPublic: true,
                        })
                  }
                  className="gap-1"
                >
                  {collection.isPublic ? (
                    <Eye className="h-3.5 w-3.5" />
                  ) : (
                    <EyeOff className="h-3.5 w-3.5" />
                  )}
                </Button>

                <Button
                  variant="outline"
                  size="sm"
                  onClick={() =>
                    collections.deleteCollection(collection.id)
                  }
                  className="gap-1"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Footer Info */}
      <div className="mt-12 rounded-lg border border-border bg-muted/30 p-6">
        <h2 className="font-semibold text-foreground mb-2">
          Using Collections
        </h2>
        <ul className="space-y-2 text-sm text-muted-foreground">
          <li>• Create collections to organize documents by topic or project</li>
          <li>• Share public collections with other researchers</li>
          <li>• Export collections for external use or backup</li>
          <li>• Add tags to collections for better organization</li>
        </ul>
      </div>
    </div>
  )
}
