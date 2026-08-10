import type { ReactNode } from "react";
import { AlertCircle, Search } from "lucide-react";
import { clsx } from "clsx";

// ─────────────────────────────────────────────
// BADGE — durum renkleri. Yeni durum adı eklerken buraya da ekleyin.
// ─────────────────────────────────────────────
const STATUS_STYLES: Record<string, string> = {
  "aktif": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "pasif": "bg-gray-100 text-gray-500 ring-1 ring-gray-200",
  "beklemede": "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
  "onaylı": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "olumlu": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "reddedildi": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "olumsuz": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "taslak": "bg-gray-100 text-gray-500 ring-1 ring-gray-200",
  "teklif": "bg-gray-100 text-gray-600 ring-1 ring-gray-200",
  "sipariş": "bg-indigo-50 text-indigo-700 ring-1 ring-indigo-200",
  "düzeltme talebi": "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
  "tamamlandı": "bg-blue-50 text-blue-700 ring-1 ring-blue-200",
  "yolda": "bg-indigo-50 text-indigo-700 ring-1 ring-indigo-200",
  "yükleniyor": "bg-violet-50 text-violet-700 ring-1 ring-violet-200",
  "iptal": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "ödendi": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "vadesi geçti": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "açık": "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
  "çözüldü": "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200",
  "yüksek": "bg-red-50 text-red-600 ring-1 ring-red-200",
  "orta": "bg-amber-50 text-amber-700 ring-1 ring-amber-200",
  "düşük": "bg-gray-100 text-gray-500 ring-1 ring-gray-200",
};

export function Badge({ label }: { label: string }) {
  return (
    <span
      className={clsx(
        "inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium font-mono tracking-wide whitespace-nowrap",
        STATUS_STYLES[label.toLowerCase()] ?? "bg-gray-100 text-gray-600 ring-1 ring-gray-200",
      )}
    >
      {label}
    </span>
  );
}

// ─────────────────────────────────────────────
// BUTTON
// ─────────────────────────────────────────────
export function Btn({
  variant = "primary",
  size = "md",
  children,
  onClick,
  className,
  disabled,
  type = "button",
}: {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "sm" | "md";
  children: ReactNode;
  onClick?: () => void;
  className?: string;
  disabled?: boolean;
  type?: "button" | "submit";
}) {
  const base =
    "inline-flex items-center gap-1.5 font-medium rounded transition-all duration-150 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-1 disabled:opacity-50 disabled:cursor-not-allowed select-none";
  const sizes = { sm: "px-2.5 py-1.5 text-xs", md: "px-3.5 py-2 text-sm" };
  const variants = {
    primary: "bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800 shadow-sm",
    secondary: "bg-white text-gray-700 border border-gray-200 hover:bg-gray-50 active:bg-gray-100",
    ghost: "text-gray-600 hover:bg-gray-100 hover:text-gray-900",
    danger: "bg-red-600 text-white hover:bg-red-700 shadow-sm",
  };
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={clsx(base, sizes[size], variants[variant], className)}
    >
      {children}
    </button>
  );
}

// ─────────────────────────────────────────────
// FORM INPUTS
// ─────────────────────────────────────────────
export function SearchInput({
  value,
  onChange,
  placeholder = "Ara...",
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
}) {
  return (
    <div className="relative">
      <Search size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none" />
      <input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="pl-8 pr-3 py-1.5 text-sm border border-gray-200 rounded-md bg-white focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:border-blue-400 w-60 transition-all"
      />
    </div>
  );
}

export function TextInput({
  value,
  onChange,
  placeholder,
  error,
  disabled,
  type = "text",
  name,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  error?: boolean;
  disabled?: boolean;
  type?: string;
  name?: string;
}) {
  return (
    <input
      type={type}
      name={name}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      disabled={disabled}
      className={clsx(
        "px-3 py-2 text-sm border rounded-md bg-white transition-all w-full",
        "focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:border-blue-400",
        error ? "border-red-400 bg-red-50/30" : "border-gray-200",
        disabled && "bg-gray-50 text-gray-400 cursor-not-allowed",
      )}
    />
  );
}

export function SelectInput({
  value,
  onChange,
  options,
  disabled,
}: {
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  disabled?: boolean;
}) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
      className={clsx(
        "px-3 py-2 text-sm border border-gray-200 rounded-md bg-white w-full",
        "focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500",
        disabled && "bg-gray-50 text-gray-400 cursor-not-allowed",
      )}
    >
      {options.map((o) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  );
}

export function TextareaInput({
  value,
  onChange,
  placeholder,
  rows = 3,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  rows?: number;
}) {
  return (
    <textarea
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      rows={rows}
      className="px-3 py-2 text-sm border border-gray-200 rounded-md bg-white w-full focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 resize-none"
    />
  );
}

export function FormField({
  label,
  required,
  error,
  hint,
  children,
}: {
  label: string;
  required?: boolean;
  error?: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-[11px] font-semibold text-gray-500 uppercase tracking-wider">
        {label}
        {required && <span className="text-red-500 ml-0.5">*</span>}
      </label>
      {children}
      {hint && !error && <span className="text-[11px] text-gray-400">{hint}</span>}
      {error && (
        <span className="text-[11px] text-red-600 flex items-center gap-1">
          <AlertCircle size={10} />
          {error}
        </span>
      )}
    </div>
  );
}

// ─────────────────────────────────────────────
// TABS
// ─────────────────────────────────────────────
export function Tabs({
  tabs,
  active,
  onChange,
  className,
}: {
  tabs: string[];
  active: string;
  onChange: (t: string) => void;
  className?: string;
}) {
  return (
    <div className={clsx("flex items-center gap-0 border-b border-gray-200 overflow-x-auto", className)}>
      {tabs.map((t) => (
        <button
          key={t}
          onClick={() => onChange(t)}
          className={clsx(
            "px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors whitespace-nowrap",
            active === t ? "border-blue-600 text-blue-600" : "border-transparent text-gray-500 hover:text-gray-700",
          )}
        >
          {t}
        </button>
      ))}
    </div>
  );
}

