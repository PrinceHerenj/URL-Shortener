import http from 'k6/http';
import { check, sleep } from 'k6';

// Benchmark configuration
export const options = {
  stages: [
    { duration: '5s', target: 20 },   // Warm-up: ramp to 20 virtual users
    { duration: '20s', target: 100 }, // Sustained load: 100 concurrent virtual users
    { duration: '5s', target: 0 },    // Ramp-down to 0
  ],
  thresholds: {
    // 95% of redirect requests must complete in under 15ms
    http_req_duration: ['p(95)<15'],
    // Less than 1% failed requests allowed
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = 'http://localhost:5050'; // Update port if your app runs on another port (e.g., 5123)

// Create one real short URL before the load begins.
export function setup() {
  const params = { headers: { 'Content-Type': 'application/json' } };
  const res = http.post(`${BASE_URL}/shorten`, JSON.stringify({ url: 'https://github.com/repo' }), params);
  check(res, { 'shorten returned 201': (r) => r.status === 201 });
  return { shortCode: res.json('shortCode') };
}

export default function (data) {
  // 1. Test Redirection Throughput (do not auto-follow redirect)
  const redirectRes = http.get(`${BASE_URL}/r/${data.shortCode}`, {
    redirects: 0,
  });

  check(redirectRes, {
    'status is 302': (r) => r.status === 302,
  });

  sleep(0.01); // 10ms pacing between iterations
}
