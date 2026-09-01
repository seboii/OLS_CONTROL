import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, useAuth } from "@/lib/auth";
import { ToastProvider } from "@/components/ui/Toast";
import { AppLayout } from "@/components/layout/AppLayout";
import { LoginPage } from "@/pages/LoginPage";
import { DashboardPage } from "@/pages/dashboard/DashboardPage";
import { CustomersPage } from "@/pages/customers/CustomersPage";
import { QuotesPage } from "@/pages/quotes/QuotesPage";
import { LoadsPage } from "@/pages/loads/LoadsPage";
import { TripsPage } from "@/pages/trips/TripsPage";
import { InvoicesPage } from "@/pages/invoices/InvoicesPage";
import { VehiclesPage } from "@/pages/vehicles/VehiclesPage";
import { UsersPage } from "@/pages/users/UsersPage";
import { SupportPage } from "@/pages/support/SupportPage";
import { ReportingPage } from "@/pages/reporting/ReportingPage";
import { FinancePage } from "@/pages/finance/FinancePage";
import { AccountingPage } from "@/pages/accounting/AccountingPage";
import { AuditLogPage } from "@/pages/audit/AuditLogPage";
import { ProfilePage } from "@/pages/ProfilePage";

/**
 * Teklif modülünü kullanmayan şirketin (Avrora) kullanıcısı adresi elle yazsa
 * bile Teklifler ekranına giremesin. Menüde sekme zaten gizli; bu, gizli
 * menünün yetki OLMADIĞI için gereken ikinci katman. Sunucu tarafı üçüncü
 * katman (RequiresOfferModule) ve asıl karar orada.
 */
function OfferModuleRoute({ children }: { children: React.ReactNode }) {
  const { loading, capabilities } = useAuth();

  // Yetenekler gelmeden yönlendirme yapılmaz, aksi hâlde ilk karede
  // herkes panele atılırdı.
  if (loading) return null;
  if (!capabilities.uses_offers) return <Navigate to="/yukler" replace />;

  return <>{children}</>;
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
          <Routes>
            <Route path="/giris" element={<LoginPage />} />
            <Route element={<AppLayout />}>
              <Route path="/" element={<Navigate to="/panel" replace />} />
              <Route path="/panel" element={<DashboardPage />} />
              <Route path="/musteriler" element={<CustomersPage />} />
              <Route
                path="/teklifler"
                element={<OfferModuleRoute><QuotesPage /></OfferModuleRoute>}
              />
              <Route path="/yukler" element={<LoadsPage />} />
              <Route path="/seferler" element={<TripsPage />} />
              <Route path="/faturalar" element={<InvoicesPage />} />
              <Route path="/araclar" element={<VehiclesPage />} />
              <Route path="/kullanicilar" element={<UsersPage />} />
              <Route path="/destek-talepleri" element={<SupportPage />} />
              <Route path="/raporlama" element={<ReportingPage />} />
              <Route path="/finans" element={<FinancePage />} />
              <Route path="/muhasebe" element={<AccountingPage />} />
              <Route path="/denetim" element={<AuditLogPage />} />
              <Route path="/hesabim" element={<ProfilePage />} />
            </Route>
            <Route path="*" element={<Navigate to="/panel" replace />} />
          </Routes>
        </ToastProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
