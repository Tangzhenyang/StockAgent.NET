import { describe, expect, it } from 'vitest';
import { readApiError } from './researchApi';

describe('readApiError', () => {
  it('reads ASP.NET Core problem details from application/problem+json responses', async () => {
    const response = new Response(
      JSON.stringify({
        title: 'PDF export failed',
        detail: 'Chromium executable was not found in the API container.',
      }),
      {
        status: 500,
        headers: { 'content-type': 'application/problem+json; charset=utf-8' },
      },
    );

    await expect(readApiError(response, 'Export PDF failed with 500')).resolves.toBe(
      'Chromium executable was not found in the API container.',
    );
  });
});
