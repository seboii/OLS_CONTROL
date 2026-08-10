import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from "react";
import { motion, AnimatePresence } from "motion/react";
import { CheckCircle, Info, X, XCircle } from "lucide-react";
import { clsx } from "clsx";

export type ToastType = "success" | "error" | "info";
interface ToastData {
  id: number;
  message: string;
  type: ToastType;
}

interface ToastContextValue {
  addToast: (message: string, type?: ToastType) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastData[]>([]);
  const toastId = useRef(0);

  const addToast = useCallback((message: string, type: ToastType = "success") => {
    const id = ++toastId.current;
    setToasts((prev) => [...prev, { id, message, type }]);
    setTimeout(() => setToasts((prev) => prev.filter((t) => t.id !== id)), 3800);
  }, []);

  const removeToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const value = useMemo(() => ({ addToast }), [addToast]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="fixed bottom-6 right-6 z-[200] flex flex-col gap-2">
        <AnimatePresence>
          {toasts.map((t) => (
            <motion.div
              key={t.id}
              initial={{ opacity: 0, y: 20, scale: 0.95 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, x: 40, scale: 0.95 }}
              transition={{ duration: 0.2, ease: "easeOut" }}
              className={clsx(
                "flex items-center gap-2.5 px-4 py-3 rounded-lg shadow-lg text-sm font-medium min-w-[260px]",
                t.type === "success" && "bg-gray-900 text-white",
                t.type === "error" && "bg-red-600 text-white",
                t.type === "info" && "bg-blue-600 text-white",
              )}
            >
              {t.type === "success" && <CheckCircle size={15} className="shrink-0" />}
              {t.type === "error" && <XCircle size={15} className="shrink-0" />}
              {t.type === "info" && <Info size={15} className="shrink-0" />}
              <span className="flex-1">{t.message}</span>
              <button onClick={() => removeToast(t.id)} className="ml-1 opacity-60 hover:opacity-100">
                <X size={13} />
              </button>
            </motion.div>
          ))}
        </AnimatePresence>
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error("useToast must be used within ToastProvider");
  return ctx;
}
