import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ResearchWorkbench } from './components/ResearchWorkbench';
import './styles.css';

const queryClient = new QueryClient();

/**
 * Root frontend application for the stock research workbench.
 */
export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ResearchWorkbench />
    </QueryClientProvider>
  );
}
