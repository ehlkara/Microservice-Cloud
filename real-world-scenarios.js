import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";

// Custom metrics
export const orderCreationRate = new Rate('order_creation_success_rate');
export const invoiceProcessingTime = new Trend('invoice_processing_time');
export const memberRegistrationCount = new Counter('member_registrations');

export const options = {
  scenarios: {
    // Senaryo 1: Normal traffic (gün içi normal yoğunluk)
    normal_traffic: {
      executor: 'constant-vus',
      vus: 10,
      duration: '2m',
      tags: { scenario: 'normal' },
      env: { SCENARIO: 'normal' }
    },
    
    // Senaryo 2: Peak hours (öğle arası yoğunluk)
    peak_hours: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 50 },
        { duration: '2m', target: 100 },
        { duration: '30s', target: 0 }
      ],
      tags: { scenario: 'peak' },
      env: { SCENARIO: 'peak' }
    },
    
    // Senaryo 3: Flash sale (ani yoğunluk artışı)
    flash_sale: {
      executor: 'ramping-arrival-rate',
      startRate: 0,
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 200,
      stages: [
        { duration: '30s', target: 10 },
        { duration: '1m', target: 100 },
        { duration: '30s', target: 0 }
      ],
      tags: { scenario: 'flash_sale' },
      env: { SCENARIO: 'flash_sale' }
    },
    
    // Senaryo 4: Soak test (uzun süre dayanıklılık)
    soak_test: {
      executor: 'constant-vus',
      vus: 20,
      duration: '5m',
      tags: { scenario: 'soak' },
      env: { SCENARIO: 'soak' }
    }
  },
  
  thresholds: {
    http_req_duration: ['p(95)<1000'], // 95% of requests under 1s
    http_req_failed: ['rate<0.05'],    // Error rate under 5%
    order_creation_success_rate: ['rate>0.95'], // 95% success rate
    invoice_processing_time: ['p(90)<2000'], // 90% under 2s
  }
};

// Base URLs
const ORDER_SERVICE = 'http://localhost:5000';
const INVOICE_SERVICE = 'http://localhost:5002';
const MEMBERSHIP_SERVICE = 'http://localhost:5003';

// Test data generators
function generateMember() {
  const names = ['Ahmet', 'Mehmet', 'Ayşe', 'Fatma', 'Ali', 'Veli', 'Zeynep', 'Elif'];
  const surnames = ['Yılmaz', 'Kaya', 'Demir', 'Şahin', 'Çelik', 'Arslan', 'Doğan', 'Aydın'];
  const domains = ['gmail.com', 'hotmail.com', 'yahoo.com', 'outlook.com'];
  
  const name = names[Math.floor(Math.random() * names.length)];
  const surname = surnames[Math.floor(Math.random() * surnames.length)];
  const domain = domains[Math.floor(Math.random() * domains.length)];
  
  return {
    name: `${name} ${surname}`,
    email: `${name.toLowerCase()}.${surname.toLowerCase()}@${domain}`,
    phone: `+90 5${Math.floor(Math.random() * 100000000).toString().padStart(8, '0')}`
  };
}

function generateOrder() {
  const products = [
    { name: 'iPhone 15 Pro', price: 55000 },
    { name: 'Samsung Galaxy S24', price: 45000 },
    { name: 'MacBook Pro M3', price: 85000 },
    { name: 'Dell XPS 13', price: 35000 },
    { name: 'iPad Pro', price: 25000 },
    { name: 'Apple Watch Series 9', price: 15000 },
    { name: 'AirPods Pro', price: 8000 },
    { name: 'PlayStation 5', price: 20000 }
  ];
  
  const product = products[Math.floor(Math.random() * products.length)];
  const quantity = Math.floor(Math.random() * 5) + 1;
  
  return {
    customerId: `customer_${Math.floor(Math.random() * 1000)}`,
    productName: product.name,
    quantity: quantity,
    price: product.price * quantity,
    shippingAddress: `İstanbul, Ankara, İzmir, Bursa, Antalya`.split(', ')[Math.floor(Math.random() * 5)]
  };
}

// Main test function
export default function() {
  const scenario = __ENV.SCENARIO || 'normal';
  
  group('Real World E-Commerce Flow', function() {
    // Senaryo bazlı test akışı
    switch(scenario) {
      case 'normal':
        normalTrafficScenario();
        break;
      case 'peak':
        peakHoursScenario();
        break;
      case 'flash_sale':
        flashSaleScenario();
        break;
      case 'soak':
        soakTestScenario();
        break;
    }
  });
}

function normalTrafficScenario() {
  group('Normal Traffic - Member Registration', function() {
    const member = generateMember();
    const response = http.post(`${MEMBERSHIP_SERVICE}/Members`, JSON.stringify(member), {
      headers: { 'Content-Type': 'application/json' }
    });
    
    check(response, {
      'member created successfully': (r) => r.status === 200 || r.status === 201
    });
    
    if (response.status === 200 || response.status === 201) {
      memberRegistrationCount.add(1);
    }
  });
  
  sleep(1);
  
  group('Normal Traffic - Order Creation', function() {
    const order = generateOrder();
    const startTime = Date.now();
    
    const response = http.post(`${ORDER_SERVICE}/Orders`, JSON.stringify(order), {
      headers: { 'Content-Type': 'application/json' }
    });
    
    const success = check(response, {
      'order created successfully': (r) => r.status === 200 || r.status === 201,
      'response time OK': (r) => r.timings.duration < 1000
    });
    
    orderCreationRate.add(success);
    
    if (success) {
      // Invoice processing time simulation
      const processingTime = Date.now() - startTime;
      invoiceProcessingTime.add(processingTime);
    }
  });
  
  sleep(2);
}

function peakHoursScenario() {
  // Peak hours - daha yoğun işlem
  group('Peak Hours - Bulk Operations', function() {
    const orders = [];
    
    // Çoklu sipariş oluşturma
    for (let i = 0; i < 3; i++) {
      const order = generateOrder();
      const response = http.post(`${ORDER_SERVICE}/Orders`, JSON.stringify(order), {
        headers: { 'Content-Type': 'application/json' }
      });
      
      orderCreationRate.add(response.status === 200 || response.status === 201);
      orders.push(response);
    }
    
    check(orders, {
      'all orders processed': (orders) => orders.every(r => r.status === 200 || r.status === 201)
    });
  });
  
  sleep(0.5);
}

function flashSaleScenario() {
  // Flash sale - çok hızlı işlem
  group('Flash Sale - High Frequency Orders', function() {
    const order = generateOrder();
    order.productName = 'Flash Sale - iPhone 15 Pro';
    order.price = 35000; // İndirimli fiyat
    
    const response = http.post(`${ORDER_SERVICE}/Orders`, JSON.stringify(order), {
      headers: { 'Content-Type': 'application/json' }
    });
    
    const success = check(response, {
      'flash sale order created': (r) => r.status === 200 || r.status === 201,
      'response under 500ms': (r) => r.timings.duration < 500
    });
    
    orderCreationRate.add(success);
  });
  
  sleep(0.1); // Çok kısa bekleme
}

function soakTestScenario() {
  // Soak test - uzun süre dayanıklılık
  group('Soak Test - Sustained Load', function() {
    // Health check
    const healthCheck = http.get(`${ORDER_SERVICE}/Health`);
    check(healthCheck, {
      'service healthy': (r) => r.status === 200
    });
    
    // Normal işlem
    const member = generateMember();
    const memberResponse = http.post(`${MEMBERSHIP_SERVICE}/Members`, JSON.stringify(member), {
      headers: { 'Content-Type': 'application/json' }
    });
    
    if (memberResponse.status === 200 || memberResponse.status === 201) {
      const order = generateOrder();
      const orderResponse = http.post(`${ORDER_SERVICE}/Orders`, JSON.stringify(order), {
        headers: { 'Content-Type': 'application/json' }
      });
      
      orderCreationRate.add(orderResponse.status === 200 || orderResponse.status === 201);
    }
  });
  
  sleep(1);
}

// Custom report generation
export function handleSummary(data) {
  const report = htmlReport(data);
  
  // Custom metrics summary
  const customSummary = {
    'test_scenarios': {
      'normal_traffic': data.metrics.http_reqs ? data.metrics.http_reqs.values.count : 0,
      'order_success_rate': data.metrics.order_creation_success_rate ? 
        data.metrics.order_creation_success_rate.values.rate : 0,
      'average_response_time': data.metrics.http_req_duration ? 
        data.metrics.http_req_duration.values.avg : 0
    }
  };
  
  return {
    'real-world-scenarios-summary.html': report,
    'custom-metrics.json': JSON.stringify(customSummary, null, 2)
  };
} 