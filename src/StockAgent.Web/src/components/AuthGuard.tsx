import { useQuery } from '@tanstack/react-query';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { getCurrentUser } from '../api/authApi';

/**
 * Protects application routes by restoring the login state from the auth cookie.
 */
export function AuthGuard() {
  const location = useLocation();
  const currentUserQuery = useQuery({
    queryKey: ['currentUser'],
    queryFn: getCurrentUser,
    retry: false,
  });

  if (currentUserQuery.isLoading) {
    return <main className="authLoading">正在恢复登录状态...</main>;
  }

  if (!currentUserQuery.data?.isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}
