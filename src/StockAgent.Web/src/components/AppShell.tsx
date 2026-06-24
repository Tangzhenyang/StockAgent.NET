import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { getCurrentUser, logout } from '../api/authApi';

/**
 * Authenticated application chrome with navigation and logout.
 */
export function AppShell() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const currentUserQuery = useQuery({
    queryKey: ['currentUser'],
    queryFn: getCurrentUser,
    retry: false,
  });
  const logoutMutation = useMutation({
    mutationFn: logout,
    onSuccess: async () => {
      queryClient.clear();
      navigate('/login', { replace: true });
    },
  });

  return (
    <div className="appShell">
      <header className="appHeader">
        <strong>Stock Research Agent</strong>
        <nav aria-label="主导航">
          <NavLink to="/">工作台</NavLink>
          <NavLink to="/history">历史记录</NavLink>
          <NavLink to="/settings">设置</NavLink>
        </nav>
        <div className="userMenu">
          <span>{currentUserQuery.data?.userName}</span>
          <button type="button" onClick={() => logoutMutation.mutate()} disabled={logoutMutation.isPending}>
            退出
          </button>
        </div>
      </header>
      <Outlet />
    </div>
  );
}
