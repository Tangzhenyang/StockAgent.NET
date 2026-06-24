import type {
  DataSourceSettings,
  DataSourceSettingsTestResponse,
  ModelSettings,
  ModelSettingsTestResponse,
  ResearchSettings,
  SaveDataSourceSettingsRequest,
  SaveModelSettingsRequest,
  SaveResearchSettingsRequest,
  UserSettings,
} from '../models';
import { apiBaseUrl } from './researchApi';

/**
 * Loads sanitized settings for the current user.
 */
export async function getUserSettings(): Promise<UserSettings> {
  const response = await fetch(`${apiBaseUrl}/api/user-settings`, {
    credentials: 'include',
  });

  if (!response.ok) {
    throw new Error(`Get user settings failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Saves current user's model provider settings.
 */
export async function saveModelSettings(request: SaveModelSettingsRequest): Promise<ModelSettings> {
  const response = await fetch(`${apiBaseUrl}/api/user-settings/model`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Save model settings failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Saves current user's research settings.
 */
export async function saveResearchSettings(request: SaveResearchSettingsRequest): Promise<ResearchSettings> {
  const response = await fetch(`${apiBaseUrl}/api/user-settings/research`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Save research settings failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Saves current user's external data source settings.
 */
export async function saveDataSourceSettings(request: SaveDataSourceSettingsRequest): Promise<DataSourceSettings> {
  const response = await fetch(`${apiBaseUrl}/api/user-settings/data-sources`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Save data source settings failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Validates whether the saved model settings are complete.
 */
export async function testModelSettings(): Promise<ModelSettingsTestResponse> {
  const response = await fetch(`${apiBaseUrl}/api/user-settings/model/test`, {
    method: 'POST',
    credentials: 'include',
  });

  if (!response.ok) {
    throw new Error(`Test model settings failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Validates whether the saved data source settings are complete.
 */
export async function testDataSourceSettings(): Promise<DataSourceSettingsTestResponse> {
  const response = await fetch(`${apiBaseUrl}/api/user-settings/data-sources/test`, {
    method: 'POST',
    credentials: 'include',
  });

  if (!response.ok) {
    throw new Error(`Test data source settings failed with ${response.status}`);
  }

  return response.json();
}
