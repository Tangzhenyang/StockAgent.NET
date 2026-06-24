import type { CurrentUser } from '../models';
import { apiBaseUrl } from './researchApi';

/**
 * Loads the current authenticated user from the HttpOnly auth cookie.
 */
export async function getCurrentUser(): Promise<CurrentUser | null> {
  const response = await fetch(`${apiBaseUrl}/api/auth/me`, {
    credentials: 'include',
  });

  if (response.status === 401) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Get current user failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Registers a local application account.
 */
export async function register(userName: string, password: string): Promise<CurrentUser> {
  const response = await fetch(`${apiBaseUrl}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ userName, password }),
  });

  if (!response.ok) {
    throw new Error(`Register failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Logs in with a persistent HttpOnly auth cookie.
 */
export async function login(userName: string, password: string): Promise<CurrentUser> {
  const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ userName, password }),
  });

  if (!response.ok) {
    throw new Error(`Login failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Logs out and clears the server auth cookie.
 */
export async function logout(): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/auth/logout`, {
    method: 'POST',
    credentials: 'include',
  });

  if (!response.ok) {
    throw new Error(`Logout failed with ${response.status}`);
  }
}
