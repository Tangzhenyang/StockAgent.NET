import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { AppShell } from './components/AppShell';
import { AuthGuard } from './components/AuthGuard';
import { HistoryPage } from './components/HistoryPage';
import { LoginPage } from './components/LoginPage';
import { ResearchWorkbench } from './components/ResearchWorkbench';
import { RegisterPage } from './components/RegisterPage';
import { SettingsPage } from './components/SettingsPage';
import './styles.css';

const queryClient = new QueryClient();

/**
 * Root frontend application for the stock research workbench.
 */
export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route element={<AuthGuard />}>
            <Route element={<AppShell />}>
              <Route index element={<ResearchWorkbench />} />
              <Route path="/history" element={<HistoryPage />} />
              <Route path="/settings" element={<SettingsPage />} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
