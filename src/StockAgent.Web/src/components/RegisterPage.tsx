import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { FormEvent } from 'react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { login, register } from '../api/authApi';

/**
 * Local account registration page.
 */
export function RegisterPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const registerMutation = useMutation({
    mutationFn: async () => {
      await register(userName, password);
      return login(userName, password);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['currentUser'] });
      navigate('/', { replace: true });
    },
  });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    registerMutation.mutate();
  };

  return (
    <main className="authPage">
      <form className="authForm" onSubmit={handleSubmit}>
        <h1>注册</h1>
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
            autoComplete="new-password"
          />
        </label>
        {registerMutation.isError && <p className="formError">注册失败，请检查用户名是否已存在。</p>}
        <button type="submit" disabled={registerMutation.isPending || !userName.trim() || password.length < 8}>
          {registerMutation.isPending ? '注册中' : '注册并登录'}
        </button>
        <p className="authSwitch">
          已有账号？<Link to="/login">登录</Link>
        </p>
      </form>
    </main>
  );
}
