import React, { useEffect, useRef, useState, type ReactNode } from 'react';
import type { ClientGridSortDirection } from '../hooks/useClientGrid';
import { Card, SectionHeader } from '../../shared/ui/primitives';
import { EmptyState } from '../../shared/ui/states';

export type OrganizationGridColumn<T> = {
  key: string;
  label: string;
  sortable?: boolean;
  className?: string;
  render: (item: T) => ReactNode;
};

export function OrganizationGridSection<T>({
  title,
  description,
  searchLabel,
  searchPlaceholder,
  searchValue,
  onSearchChange,
  filters,
  createLabel,
  actionsLabel,
  pageLabel,
  previousLabel,
  nextLabel,
  pageSizeLabel,
  onCreate,
  columns,
  items,
  getRowKey,
  emptyText,
  sortKey,
  sortDirection,
  onSort,
  page,
  pageCount,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions,
  rangeStart,
  rangeEnd,
  totalCount,
  renderRowActions,
  onRowClick
}: {
  title: string;
  description?: string;
  searchLabel: string;
  searchPlaceholder: string;
  searchValue: string;
  onSearchChange: (value: string) => void;
  filters?: ReactNode;
  createLabel?: string;
  actionsLabel: string;
  pageLabel: (page: number, pageCount: number) => string;
  previousLabel: string;
  nextLabel: string;
  pageSizeLabel: (pageSize: number) => string;
  onCreate?: () => void;
  columns: OrganizationGridColumn<T>[];
  items: T[];
  getRowKey: (item: T) => string;
  emptyText: string;
  sortKey: string;
  sortDirection: ClientGridSortDirection;
  onSort: (sortKey: string) => void;
  page: number;
  pageCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  pageSizeOptions: number[];
  rangeStart: number;
  rangeEnd: number;
  totalCount: number;
  renderRowActions?: (item: T, closeMenu: () => void) => ReactNode;
  onRowClick?: (item: T) => void;
}) {
  const [menuRowId, setMenuRowId] = useState('');
  const actionMenuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!menuRowId) {
      return undefined;
    }

    const onMouseDown = (event: MouseEvent) => {
      if (actionMenuRef.current && !actionMenuRef.current.contains(event.target as Node)) {
        setMenuRowId('');
      }
    };

    document.addEventListener('mousedown', onMouseDown);
    return () => document.removeEventListener('mousedown', onMouseDown);
  }, [menuRowId]);

  const hasActions = Boolean(renderRowActions);

  return (
    <section className="space-y-3">
      <SectionHeader
        title={title}
        description={description}
        action={onCreate && createLabel ? (
          <button type="button" className="sk-btn sk-btn-primary inline-flex items-center gap-2" onClick={onCreate}>
            <CreateIcon className="h-3.5 w-3.5" />
            {createLabel}
          </button>
        ) : undefined}
      />
      <Card className="sk-user-management">
        <div className="sk-user-management-panel mt-0 rounded-none border-0 border-b border-slate-200 bg-slate-50 p-3">
          <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_auto]">
            <div className="grid gap-3 md:grid-cols-2">
              <div className="flex flex-col gap-1">
                <label className="sk-label">{searchLabel}</label>
                <input
                  className="sk-input"
                  value={searchValue}
                  placeholder={searchPlaceholder}
                  onChange={(event) => onSearchChange(event.target.value)}
                />
              </div>
              {filters}
            </div>
            <div className="sk-user-management-controls-inline self-end justify-self-end">
              <span>{rangeStart}-{rangeEnd} / {totalCount}</span>
              <select className="sk-input !w-auto text-xs" value={pageSize} onChange={(event) => onPageSizeChange(Number(event.target.value))}>
                {pageSizeOptions.map((size) => (
                  <option key={size} value={size}>
                    {pageSizeLabel(size)}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>
        {items.length === 0 ? (
          <div className="p-4">
            <EmptyState text={emptyText} />
          </div>
        ) : (
          <>
            <div className="sk-table-wrap mt-0 overflow-x-auto border-0 rounded-none">
              <table className="sk-table sk-user-management-table sk-sticky">
                <thead>
                  <tr className="border-b text-left">
                    {columns.map((column) => (
                      <th
                        key={column.key}
                        className={column.sortable ? `sk-sortable-th ${column.className ?? ''}`.trim() : column.className}
                        onClick={column.sortable ? () => onSort(column.key) : undefined}
                      >
                        {column.label}
                        {column.sortable ? <SortIndicator active={sortKey === column.key} direction={sortDirection} /> : null}
                      </th>
                    ))}
                    {hasActions ? <th>{actionsLabel}</th> : null}
                  </tr>
                </thead>
                <tbody>
                  {items.map((item, index) => {
                    const rowId = getRowKey(item);
                    const menuOpen = menuRowId === rowId;
                    const openUpward = index >= items.length - 1;
                    return (
                      <tr
                        key={rowId}
                        className={`sk-user-management-row border-b ${onRowClick ? 'sk-user-management-row-clickable' : ''}`.trim()}
                        onClick={() => onRowClick?.(item)}
                      >
                        {columns.map((column) => (
                          <td key={column.key} className={column.className}>
                            {column.render(item)}
                          </td>
                        ))}
                        {hasActions ? (
                          <td onClick={(event) => event.stopPropagation()}>
                            <div className="sk-action-menu" ref={menuOpen ? actionMenuRef : undefined}>
                              <button
                                type="button"
                                className="sk-action-menu-trigger"
                                aria-label={actionsLabel}
                                onClick={() => setMenuRowId(menuOpen ? '' : rowId)}
                              >
                                <ThreeDotsIcon className="h-4 w-4" />
                              </button>
                              {menuOpen ? (
                                <div className={`sk-action-menu-dropdown ${openUpward ? 'sk-action-menu-dropdown-up' : ''}`.trim()}>
                                  {renderRowActions?.(item, () => setMenuRowId(''))}
                                </div>
                              ) : null}
                            </div>
                          </td>
                        ) : null}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <div className="sk-user-management-footer flex flex-wrap items-center justify-between gap-3 px-3 pb-3">
              <p className="text-xs text-slate-500">
                {pageLabel(page, pageCount)}
              </p>
              <div className="flex gap-2">
                <button type="button" className="sk-btn sk-btn-secondary text-xs" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
                  {previousLabel}
                </button>
                <button type="button" className="sk-btn sk-btn-secondary text-xs" disabled={page >= pageCount} onClick={() => onPageChange(page + 1)}>
                  {nextLabel}
                </button>
              </div>
            </div>
          </>
        )}
      </Card>
    </section>
  );
}

function SortIndicator({ active, direction }: { active: boolean; direction: ClientGridSortDirection }) {
  return <span className="sk-sort-indicator">{active ? (direction === 'asc' ? '▲' : '▼') : '↕'}</span>;
}

function ThreeDotsIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor" aria-hidden="true">
      <circle cx="12" cy="5" r="1.5" />
      <circle cx="12" cy="12" r="1.5" />
      <circle cx="12" cy="19" r="1.5" />
    </svg>
  );
}

function CreateIcon({ className = 'h-4 w-4' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path d="M12 5v14M5 12h14" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}
