import http from 'k6/http';
import { check, sleep } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";

export const options = {
  scenarios: {
    load_test: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 50 },    // Ramp-up to 50 VUs
        { duration: '3m', target: 50 },    // Stay at 50 VUs for 3 minutes
        { duration: '1m', target: 100 },   // Ramp-up to 100 VUs
        { duration: '3m', target: 100 },   // Stay at 100 VUs for 3 minutes
        { duration: '1m', target: 0 },     // Ramp-down to 0 VUs
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    http_req_failed: ['rate<0.01'],   // Error rate should be less than 1%
  },
};

function generateRandomOrder() {
  const now = new Date();
  const randomDays = Math.floor(Math.random() * 30); // Random days in the past 30 days
  const productionDate = new Date(now.setDate(now.getDate() - randomDays));
  
  return {
    productId: Math.floor(Math.random() * 1000) + 1,
    quantity: Math.floor(Math.random() * 10) + 1,
    productionDate: productionDate.toISOString(),
    customerId: Math.floor(Math.random() * 100) + 1
  };
}

export default function () {
  const order = generateRandomOrder();
  const response = http.post('http://localhost:5000/api/orders', JSON.stringify(order), {
    headers: {
      'Content-Type': 'application/json',
    },
  });

  check(response, {
    'is status 200': (r) => r.status === 200,
    'has order id': (r) => r.json('id') !== undefined,
  });

  sleep(0.1); // Reduced sleep time to 100ms
}

export function handleSummary(data) {
  return {
    "summary.html": htmlReport(data),
  };
} 