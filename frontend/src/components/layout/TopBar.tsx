import { useEffect, useRef, useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { useNavigate } from "react-router-dom";
import { ChevronDown, LogOut, Menu, Settings, User } from "lucide-react";
import { useAuth } from "@/lib/auth";

function initials(name: string, surname: string) {
  return `${name.charAt(0)}${surname.charAt(0)}`.toUpperCase();
}

export function TopBar({ moduleLabel, onMenuToggle }: { moduleLabel: string; onMenuToggle: () => void }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [userOpen, setUserOpen] = useState(false);
  const userRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const h = (e: MouseEvent) => {
      if (userRef.current && !userRef.current.contains(e.target as Node)) setUserOpen(false);
    };
    document.addEventListener("mousedown", h);
    return () => document.removeEventListener("mousedown", h);
  }, []);

  if (!user) return null;

  return (
    <div className="h-12 bg-white border-b border-gray-200 flex items-center px-4 gap-3 shrink-0 z-30">
      <button onClick={onMenuToggle} className="lg:hidden p-1.5 rounded hover:bg-gray-100 text-gray-500">
        <Menu size={18} />
      </button>

      <div className="flex items-center gap-1.5 text-sm text-gray-500 min-w-0">
        <span className="text-gray-400 text-xs hidden sm:block">OLS Lojistik</span>
        <span className="text-gray-300 hidden sm:block">/</span>
        <span className="font-medium text-gray-800 truncate">{moduleLabel}</span>
      </div>

      <div className="flex-1" />

      <div className="relative" ref={userRef}>
        <button
          onClick={() => setUserOpen((o) => !o)}
          className="flex items-center gap-2 pl-2 pr-2.5 py-1 rounded-lg hover:bg-gray-100 transition-colors"
        >
          <div className="w-6 h-6 rounded-full bg-blue-600 flex items-center justify-center text-[10px] text-white font-bold">
            {initials(user.name, user.surname)}
          </div>
          <span className="text-xs font-medium text-gray-700 hidden sm:block">{user.name}</span>
          <ChevronDown size={12} className="text-gray-400 hidden sm:block" />
        </button>
        <AnimatePresence>
          {userOpen && (
            <motion.div
              initial={{ opacity: 0, scale: 0.92, y: -4 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.92 }}
              transition={{ duration: 0.12, ease: "easeOut" }}
              className="absolute right-0 top-10 w-48 bg-white rounded-xl shadow-xl border border-gray-200 py-1 z-50"
            >
              <div className="px-3 py-2.5 border-b border-gray-100">
                <p className="text-xs font-semibold text-gray-800">
                  {user.name} {user.surname}
                </p>
                <p className="text-[10px] text-gray-500">{user.email}</p>
              </div>
              <button
                onClick={() => {
                  setUserOpen(false);
                  navigate("/hesabim");
                }}
                className="flex items-center gap-2 w-full px-3 py-2 text-xs hover:bg-gray-50 transition-colors text-gray-700"
              >
                <User size={12} />
                Profil
              </button>
              <button
                onClick={() => {
                  setUserOpen(false);
                  navigate("/hesabim");
                }}
                className="flex items-center gap-2 w-full px-3 py-2 text-xs hover:bg-gray-50 transition-colors text-gray-700"
              >
                <Settings size={12} />
                Ayarlar
              </button>
              <button
                onClick={() => logout()}
                className="flex items-center gap-2 w-full px-3 py-2 text-xs hover:bg-gray-50 transition-colors text-red-600"
              >
                <LogOut size={12} />
                Çıkış Yap
              </button>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  );
}
