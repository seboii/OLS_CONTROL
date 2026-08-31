import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "@/lib/auth";
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
import { AuditLogPage } from "@/pages/audit/AuditLogPage";
import { ProfilePage } from "@/pages/ProfilePage";

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
              <Route path="/teklifler" element={<QuotesPage />} />
              <Route path="/yukler" element={<LoadsPage />} />
              <Route path="/seferler" element={<TripsPage />} />
              <Route path="/faturalar" element={<InvoicesPage />} />
              <Route path="/araclar" element={<VehiclesPage />} />
              <Route path="/kullanicilar" element={<UsersPage />} />
              <Route path="/destek-talepleri" element={<SupportPage />} />
              <Route path="/raporlama" element={<ReportingPage />} />
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
