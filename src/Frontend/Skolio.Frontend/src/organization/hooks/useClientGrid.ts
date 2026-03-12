import { useEffect, useMemo, useState } from 'react';

export type ClientGridSortDirection = 'asc' | 'desc';

type UseClientGridOptions<T> = {
  items: T[];
  getSearchText: (item: T) => string;
  sorters: Record<string, (left: T, right: T) => number>;
  initialSortKey: string;
  initialSortDirection?: ClientGridSortDirection;
  initialPageSize?: number;
  resetKeys?: readonly unknown[];
};

export function useClientGrid<T>({
  items,
  getSearchText,
  sorters,
  initialSortKey,
  initialSortDirection = 'asc',
  initialPageSize = 10,
  resetKeys = []
}: UseClientGridOptions<T>) {
  const [search, setSearch] = useState('');
  const [sortKey, setSortKey] = useState(initialSortKey);
  const [sortDirection, setSortDirection] = useState<ClientGridSortDirection>(initialSortDirection);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);

  useEffect(() => {
    setPage(1);
  }, [search, pageSize, ...resetKeys]);

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return items;
    }

    return items.filter((item) => getSearchText(item).toLowerCase().includes(query));
  }, [getSearchText, items, search]);

  const sortedItems = useMemo(() => {
    const comparer = sorters[sortKey];
    if (!comparer) {
      return filteredItems;
    }

    return [...filteredItems].sort((left, right) => {
      const result = comparer(left, right);
      return sortDirection === 'asc' ? result : -result;
    });
  }, [filteredItems, sortDirection, sortKey, sorters]);

  const totalCount = sortedItems.length;
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
  const safePage = Math.min(page, pageCount);

  useEffect(() => {
    if (page !== safePage) {
      setPage(safePage);
    }
  }, [page, safePage]);

  const pageItems = useMemo(() => {
    const start = (safePage - 1) * pageSize;
    return sortedItems.slice(start, start + pageSize);
  }, [pageSize, safePage, sortedItems]);

  const rangeStart = totalCount === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const rangeEnd = totalCount === 0 ? 0 : Math.min(safePage * pageSize, totalCount);

  const requestSort = (nextSortKey: string) => {
    if (nextSortKey === sortKey) {
      setSortDirection((current) => (current === 'asc' ? 'desc' : 'asc'));
      return;
    }

    setSortKey(nextSortKey);
    setSortDirection('asc');
  };

  return {
    search,
    setSearch,
    sortKey,
    sortDirection,
    requestSort,
    page,
    setPage,
    pageSize,
    setPageSize,
    totalCount,
    pageCount,
    rangeStart,
    rangeEnd,
    filteredItems,
    pageItems
  };
}
