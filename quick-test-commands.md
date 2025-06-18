# 🚀 Düzeltilmiş Test Komutları

## ✅ **Doğru Container İsimleri**
```bash
# MongoDB container ismi
docker exec -it project-mongodb-1 mongo SnapshotDb --eval "db.LocalStates.find().pretty()"

# Tüm container'ları listele
docker-compose ps
```

## 📸 **Chandy-Lamport Snapshot Tests**
```bash
# 1. Snapshot başlat
curl -X POST http://localhost:5004/Snapshot/start

# Örnek response:
# {"message":"Snapshot started","snapshotId":"123e4567-e89b-12d3-a456-426614174000","initiatorNode":"http://localhost:5000"}

# 2. En son snapshot'ları listele
curl http://localhost:5004/Snapshot/latest

# 3. Specific snapshot status (gerçek snapshotId kullan)
curl http://localhost:5004/Snapshot/status/123e4567-e89b-12d3-a456-426614174000

# 4. Health check
curl http://localhost:5004/health
```

## 🔗 **DHT (Chord Ring) Tests**
```bash
# DHT durumu kontrol et
curl http://localhost:5003/dht/status

# Node ekle
curl -X POST http://localhost:5003/dht/join \
     -H "Content-Type: application/json" \
     -d '{"nodeId":"test-node-1","address":"http://localhost:5005"}'

# Service lookup
curl http://localhost:5003/dht/lookup/order-service

# Node çıkar
curl -X DELETE http://localhost:5003/dht/leave/test-node-1
```

## 📡 **Gossip Protocol Tests**
```bash
# Membership durumu
curl http://localhost:5003/Members/membership/status

# Gossip message gönder
curl -X POST http://localhost:5003/Members/gossip \
     -H "Content-Type: application/json" \
     -d '{"nodeId":"test-node","timestamp":"2024-01-01T00:00:00Z","queueDepth":5,"cpuUsage":25.5}'

# Broadcast message
curl -X POST http://localhost:5003/Members/gossip/broadcast \
     -H "Content-Type: application/json" \
     -d '{"message":"test-broadcast","timestamp":"2024-01-01T00:00:00Z"}'

# Gossip logs izle
docker-compose logs membership-service | grep -i gossip
```

## 🏥 **Health Checks**
```bash
# Tüm servisler
curl http://localhost:5000/Health  # Order Service
curl http://localhost:5002/Health  # Invoice Service  
curl http://localhost:5003/Health  # Membership Service
curl http://localhost:5004/Health  # Snapshot Coordinator

# Service durumları
docker-compose ps
```

## 💾 **Database Tests**
```bash
# MongoDB bağlantısı test et
docker exec -it project-mongodb-1 mongo --eval "db.adminCommand('ping')"

# Snapshot verilerini görüntüle
docker exec -it project-mongodb-1 mongo SnapshotDb --eval "db.LocalStates.find().pretty()"

# Members verilerini görüntüle
docker exec -it project-mongodb-1 mongo MembershipDb --eval "db.Members.find().pretty()"

# Orders verilerini görüntüle
docker exec -it project-mongodb-1 mongo OrderDb --eval "db.Orders.find().pretty()"
```

## 🔄 **Container Management**
```bash
# Servisleri yeniden başlat
docker-compose restart

# Snapshot coordinator'ı yeniden build et
docker-compose up -d --build snapshot-coordinator

# Logları takip et
docker-compose logs -f snapshot-coordinator
docker-compose logs -f membership-service
```

## 📊 **Monitoring**
```bash
# Prometheus metrics
curl http://localhost:9090/api/v1/query?query=up

# Grafana dashboard
open http://localhost:3000  # admin/admin123

# RabbitMQ management
open http://localhost:15672  # admin/admin123
```

## 🎯 **Complete Test Sequence**
```bash
#!/bin/bash
echo "🚀 Starting complete distributed systems test..."

# 1. Health checks
echo "🏥 Health checks..."
curl -s http://localhost:5000/Health && echo " ✅ Order Service"
curl -s http://localhost:5002/Health && echo " ✅ Invoice Service"
curl -s http://localhost:5003/Health && echo " ✅ Membership Service"
curl -s http://localhost:5004/Health && echo " ✅ Snapshot Coordinator"

# 2. DHT tests
echo "🔗 DHT tests..."
curl -s http://localhost:5003/dht/status | jq '.status'

# 3. Gossip tests
echo "📡 Gossip tests..."
curl -s http://localhost:5003/Members/membership/status | jq '.totalNodes'

# 4. Snapshot tests
echo "📸 Snapshot tests..."
SNAPSHOT_RESPONSE=$(curl -s -X POST http://localhost:5004/Snapshot/start)
SNAPSHOT_ID=$(echo $SNAPSHOT_RESPONSE | jq -r '.snapshotId')
echo "Snapshot ID: $SNAPSHOT_ID"

sleep 5
curl -s http://localhost:5004/Snapshot/status/$SNAPSHOT_ID | jq '.status'

echo "🎉 All tests completed!"
```

## ⚠️ **Troubleshooting**

### Container İsimleri
```bash
# Gerçek container isimlerini bul
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Status}}"
```

### Port Kontrolü
```bash
# Port'ların açık olduğunu kontrol et
netstat -tulpn | grep -E ":(5000|5002|5003|5004)"
```

### Log Debugging
```bash
# Spesifik hata logları
docker-compose logs snapshot-coordinator | grep -i error
docker-compose logs membership-service | grep -i error
``` 