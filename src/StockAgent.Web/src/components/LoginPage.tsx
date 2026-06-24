import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { FormEvent } from 'react';
import { useState } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { getCurrentUser, login } from '../api/authApi';

/**
 * Local account login page.
 */
export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const currentUser = queryClient.getQueryData<Awaited<ReturnType<typeof getCurrentUser>>>(['currentUser']);
  const loginMutation = useMutation({
    mutationFn: () => login(userName, password),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['currentUser'] });
      const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/';
      navigate(from, { replace: true });
    },
  });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    loginMutation.mutate();
  };

  if (currentUser?.isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <main className="authPage">
      <form className="authForm" onSubmit={handleSubmit}>
        <h1>登录</h1>
        <label>
          用户名
          <input value={userName} onChange={(event) => setUserName(event.target.value)} autoComplete="username" />
        </label>
        <label>
          密码
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="current-password"
          />
        </label>
        {loginMutation.isError && <p className="formError">用户名或密码不正确。</p>}
        <button type="submit" disabled={loginMutation.isPending || !userName.trim() || !password}>
          {loginMutation.isPending ? '登录中' : '登录'}
        </button>
        <p className="authSwitch">
          还没有账号？<Link to="/register">注册</Link>
        </p>
      </form>
    </main>
  );
}
