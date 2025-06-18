# 🧪 Gerçek Dünya Senaryoları Test Rehberi

Bu rehber, microservices projenizde gerçek dünya senaryolarını test etmek için oluşturulmuş kapsamlı test suite'inin kullanımını açıklar.

## 📋 Test Türleri

### 1. 🔧 Integration Tests (C#)
**Dosya:** `RealWorldScenarios.Tests.cs` & `TestRunner.cs`

Kapsamlı entegrasyon testleri:
- E-ticaret sipariş workflow'u (Müşteri kaydı → Sipariş → Fatura)
- Yüksek yoğunluk sipariş işleme
- Error handling ve resilience
- Message queue flow
- Data consistency
- Performance metrics

### 2. ⚡ Load Testing (K6)
**Dosya:** `real-world-scenarios.js`

Farklı yoğunluk senaryoları:
- **Normal Traffic:** Günlük normal kullanım (10 VU, 2 dakika)
- **Peak Hours:** Yoğun saatler (0→100 VU, ramping)
- **Flash Sale:** Ani yoğunluk artışı (arrival rate based)
- **Soak Test:** Uzun süre dayanıklılık (20 VU, 5 dakika)

### 3. 🔥 Chaos Engineering
**Dosya:** `chaos-testing.sh`

Gerçek dünya hatalarını simüle eder:
- Service down recovery
- Database connectivity issues
- Message queue failures
- High CPU/Memory load
- Network latency simulation
- Data corruption handling

### 4. 📸 Distributed Systems Testing
**Chandy-Lamport Snapshot, DHT, Gossip Protocol**

Test edilebilir dağıtık sistem algoritmaları:
- **DHT Service Discovery:** Chord ring based service lookup
- **Gossip Protocol:** Node failure detection ve membership
- **Chandy-Lamport Snapshots:** Distributed state management

## 🚀 Hızlı Başlangıç

### Windows Kullanıcıları
```powershell
# PowerShell'i Administrator olarak açın
./RunTests.ps1
```

### Linux/Mac Kullanıcıları
```bash
# Docker environment'ı başlatın
docker-compose up -d

# Servislerin hazır olmasını bekleyin (30 saniye)
sleep 30

# Test suite'i çalıştırın
# Seçenek 1: C# Integration Tests
dotnet run TestRunner.cs

# Seçenek 2: K6 Load Tests
k6 run real-world-scenarios.js

# Seçenek 3: Chaos Engineering
chmod +x chaos-testing.sh
./chaos-testing.sh
```

## 📊 Test Senaryoları Detayı

### 🛍️ E-Ticaret Workflow Testi
```
1. Yeni müşteri kaydı (MembershipService)
   ↓
2. Sipariş oluşturma (OrderService)
   ↓
3. RabbitMQ event processing
   ↓
4. Otomatik fatura oluşturma (InvoiceService)
   ↓
5. Veri tutarlılığı kontrolü
```

### ⚡ Performance Test Senaryoları

#### Normal Traffic (Baseline)
- **Yoğunluk:** 10 eşzamanlı kullanıcı
- **Süre:** 2 dakika
- **Hedef:** Response time < 1000ms
- **Test edilen:** Normal CRUD operasyonları

#### Peak Hours (Yoğun Saatler)
- **Yoğunluk:** 0→50→100→0 kullanıcı (ramping)
- **Süre:** 3 dakika
- **Hedef:** Error rate < 5%
- **Test edilen:** Çoklu sipariş işleme

#### Flash Sale (Ani Yoğunluk)
- **Yoğunluk:** 0→100 req/sec (arrival rate)
- **Süre:** 2 dakika  
- **Hedef:** Response time < 500ms
- **Test edilen:** Yüksek frekanslı siparişler

#### Soak Test (Dayanıklılık)
- **Yoğunluk:** 20 sabit kullanıcı
- **Süre:** 5 dakika
- **Hedef:** Memory leak yok
- **Test edilen:** Uzun süre stabilite

### 🔥 Chaos Engineering Senaryoları

#### Service Down Recovery
```bash
# OrderService'i durdur
docker-compose stop order-service

# Sistem davranışını gözlemle
curl http://localhost:5002/Health  # Invoice Service
curl http://localhost:5003/Health  # Membership Service

# Servisi tekrar başlat
docker-compose start order-service
```

#### Database Connectivity Issues
```bash
# MongoDB'yi durdur
docker-compose stop mongodb

# Database bağlantı hatalarını test et
curl -X POST http://localhost:5000/Orders \
     -H "Content-Type: application/json" \
     -d '{"customerId":"test","productName":"Test","quantity":1,"price":100}'

# MongoDB'yi tekrar başlat
docker-compose start mongodb
```

#### Message Queue Failures
```bash
# RabbitMQ'yu durdur
docker-compose stop rabbitmq

# Event messaging olmadan işlem testleri
curl -X POST http://localhost:5000/Orders [data]
curl -X POST http://localhost:5003/Members [data]

# RabbitMQ'yu tekrar başlat
docker-compose start rabbitmq
```

## 🔬 Dağıtık Sistem Algoritma Testleri

### 📸 Chandy-Lamport Snapshot Testing

```bash
# Snapshot Coordinator başlat
docker-compose up -d snapshot-coordinator

# Distributed snapshot başlat
curl -X POST http://localhost:5004/Snapshot/start

# Snapshot durumunu kontrol et
curl http://localhost:5004/Snapshot/latest

# Specific snapshot status
curl http://localhost:5004/Snapshot/status/{snapshotId}
```

**Chandy-Lamport Algorithm Test Workflow:**
```
1. Coordinator → Random Node'a marker gönder
2. Node → Local state capture et
3. Node → Diğer tüm node'lara marker forward et
4. Tüm node'lar → State'lerini MongoDB'ye kaydet
5. Coordinator → Complete snapshot'ı topla
```

### 🔗 DHT (Chord Ring) Testing

```bash
# Service Discovery test
curl http://localhost:5003/Members  # DHT üzerinden service lookup

# Node join/leave simulation
# Add new node to ring
curl -X POST http://localhost:5003/dht/join \
     -H "Content-Type: application/json" \
     -d '{"nodeId":"new-node-1","address":"http://localhost:5005"}'

# Remove node from ring
curl -X DELETE http://localhost:5003/dht/leave/new-node-1

# Hash ring durumunu görüntüle
curl http://localhost:5003/dht/status
```

### 📡 Gossip Protocol Testing

```bash
# Gossip heartbeat messages'ları gözlemle
docker-compose logs membership-service | grep -i gossip

# Node failure simulation
docker-compose stop membership-service
sleep 35  # Wait for failure detection timeout

# Node recovery
docker-compose start membership-service

# Membership list durumu
curl http://localhost:5003/membership/status
```

## 📈 Metrik ve Monitoring

### Test Sırasında İzlenmesi Gerekenler

#### Prometheus Metrics
```bash
# Service health
curl http://localhost:9090/api/v1/query?query=up

# Response times
curl http://localhost:9090/api/v1/query?query=http_request_duration_seconds

# Error rates
curl http://localhost:9090/api/v1/query?query=http_requests_total

# Gossip protocol metrics
curl http://localhost:9090/api/v1/query?query=app_gossip_rtt_seconds
```

#### Docker Container Stats
```bash
# Resource usage
docker stats --no-stream

# Container health
docker-compose ps
```

#### Application Logs
```bash
# Service logs
docker-compose logs order-service
docker-compose logs invoice-service
docker-compose logs membership-service
docker-compose logs snapshot-coordinator

# RabbitMQ management
open http://localhost:15672  # admin/admin123
```

## 🎯 Test Kriterleri ve Beklenen Sonuçlar

### Performance Thresholds
```javascript
{
  http_req_duration: ['p(95)<1000'],     // 95% < 1 saniye
  http_req_failed: ['rate<0.05'],        // Error rate < 5%
  order_creation_success_rate: ['rate>0.95'], // 95% başarı
  invoice_processing_time: ['p(90)<2000'], // 90% < 2 saniye
  gossip_rtt: ['p(95)<50'],              // 95% gossip RTT < 50ms
  snapshot_duration: ['<5000']           // Snapshot < 5 saniye
}
```

### Health Check Expectations
- **Order Service:** http://localhost:5000/Health → 200 OK
- **Invoice Service:** http://localhost:5002/Health → 200 OK  
- **Membership Service:** http://localhost:5003/Health → 200 OK
- **Snapshot Coordinator:** http://localhost:5004/Health → 200 OK
- **RabbitMQ Management:** http://localhost:15672 → Admin panel
- **Prometheus:** http://localhost:9090 → Metrics endpoint
- **Grafana:** http://localhost:3000 → Dashboard (admin/admin123)

### Distributed Systems Expectations
- **DHT Lookup:** < 200ms service discovery
- **Gossip Failure Detection:** < 30 saniye node failure detection
- **Snapshot Completion:** < 5 saniye tüm node'lardan state collection
- **Node Join/Leave:** < 500ms ring restructuring

### Chaos Recovery Expectations
- **Service Recovery:** < 30 saniye
- **Database Reconnection:** < 30 saniye
- **Message Queue Recovery:** < 30 saniye
- **Data Consistency:** Sonraki işlemlerde tutarlı
- **No Data Loss:** Kritik veri kaybı olmamalı

## 🛠️ Troubleshooting

### Sık Karşılaşılan Sorunlar

#### "Connection refused" Hataları
```bash
# Servislerin başlamasını bekleyin
docker-compose ps
sleep 30

# Port'ların açık olduğunu kontrol edin
netstat -tulpn | grep :5000
netstat -tulpn | grep :5002
netstat -tulpn | grep :5003
netstat -tulpn | grep :5004  # Snapshot Coordinator
```

#### Memory/CPU Sorunları
```bash
# Container resource'larını kontrol edin
docker stats

# System resources
free -h    # Linux
Get-ComputerInfo | Select-Object TotalPhysicalMemory # Windows
```

#### Database Bağlantı Sorunları
```bash
# MongoDB container'ı kontrol edin
docker-compose logs mongodb

# MongoDB connection test
docker exec -it project_mongodb_1 mongo --eval "db.adminCommand('ismaster')"
```

#### Snapshot Issues
```bash
# SnapshotCoordinator logs
docker-compose logs snapshot-coordinator

# MongoDB snapshot collection
docker exec -it project_mongodb_1 mongo SnapshotDb --eval "db.LocalStates.find().pretty()"
```

## 📊 Test Raporu ve Analiz

### K6 Raporları
Test tamamlandığında şu dosyalar oluşur:
- `real-world-scenarios-summary.html` - Detaylı HTML rapor
- `custom-metrics.json` - Özel metrik verileri
- `summary.html` - Genel özet

### Grafana Dashboard'ları
- **Microservices Overview:** Genel sistem durumu
- **Performance Metrics:** Response time, throughput
- **Error Tracking:** Hata oranları ve tipleri
- **Resource Usage:** CPU, Memory, Network
- **Distributed Systems:** DHT, Gossip, Snapshot metrics

### Log Analizi
```bash
# Critical errors
docker-compose logs | grep ERROR

# Performance warnings
docker-compose logs | grep "slow\|timeout\|delay"

# Success metrics
docker-compose logs | grep "success\|completed\|processed"

# Distributed system events
docker-compose logs | grep -E "gossip|snapshot|dht|chord"
```

## 🎯 İleri Seviye Test Senaryoları

### Custom Test Senaryoları Oluşturma

#### Yeni K6 Test Senaryosu
```javascript
// custom-scenario.js
export const options = {
  scenarios: {
    custom_test: {
      executor: 'constant-vus',
      vus: 20,
      duration: '3m',
    }
  }
};

export default function() {
  // Özel test logic'iniz
}
```

#### Yeni Chaos Test
```bash
# custom-chaos.sh
#!/bin/bash
echo "Custom chaos scenario"

# Özel chaos logic'iniz
```

#### Distributed Algorithm Testing
```bash
# DHT performance test
for i in {1..100}; do
  curl -s http://localhost:5003/dht/lookup/service-$i
done

# Gossip propagation test
curl -X POST http://localhost:5003/gossip/broadcast \
     -d '{"message":"test-broadcast","timestamp":"'$(date -Iseconds)'"}'
```

### CI/CD Entegrasyonu
```yaml
# .github/workflows/real-world-tests.yml
name: Real World Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Run Real World Tests
        run: |
          docker-compose up -d
          sleep 30
          k6 run real-world-scenarios.js
          ./chaos-testing.sh
          # Test distributed algorithms
          curl -X POST http://localhost:5004/Snapshot/start
          sleep 10
          curl http://localhost:5004/Snapshot/latest
```

## 📞 Destek ve Katkı

Test senaryolarını geliştirmek için:
1. Yeni test senaryoları önerebilirsiniz
2. Mevcut testleri optimize edebilirsiniz  
3. Yeni chaos engineering senaryoları ekleyebilirsiniz
4. Performance threshold'larını ayarlayabilirsiniz
5. Distributed algorithm testlerini genişletebilirsiniz

---

**🎉 Happy Testing!** Gerçek dünya senaryolarınızı başarıyla test etmeniz dileğiyle! 