import type { ReactNode } from "react";
import { SearchInput } from "./primitives";

export function ModulePage({
  title,
  action,
  search,
  searchPlaceholder,
  onSearchChange,
  filters,
  children,
}: {
  title: string;
  action?: ReactNode;
  search: string;
  searchPlaceholder?: string;
  onSearchChange: (v: string) => void;
  filters?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between px-6 py-3.5 bg-white border-b border-gray-200 shrink-0 gap-3 flex-wrap">
        <h1 className="text-base font-semibold text-gray-900">{title}</h1>
        <div className="flex items-center gap-2 flex-wrap">
          <SearchInput value={search} onChange={onSearchChange} placeholder={searchPlaceholder} />
          {filters}
          {action}
        </div>
      </div>
      <div className="flex-1 overflow-y-auto">{children}</div>
    </div>
  );
}
