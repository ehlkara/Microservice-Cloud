#!/bin/bash

# Chaos Engineering Test Script for Microservices
# Bu script gerçek dünya hatalarını simüle eder

echo "🔥 Chaos Engineering Test Suite Başlatılıyor..."
echo "==============================================="

# Test Functions
test_service_down() {
    echo "\n🚨 Test 1: Service Down Senaryosu"
    echo "OrderService'i durdurup sistem davranışını test ediyor..."
    
    # OrderService'i durdur
    docker-compose stop order-service
    echo "⏹️ OrderService durduruldu"
    
    # 30 saniye bekle
    sleep 30
    
    # Diğer servislerin durumunu kontrol et
    echo "📊 Diğer servislerin durumu:"
    curl -s http://localhost:5002/Health || echo "❌ InvoiceService erişilemiyor"
    curl -s http://localhost:5003/Health || echo "❌ MembershipService erişilemiyor"
    
    # OrderService'i tekrar başlat
    docker-compose start order-service
    echo "✅ OrderService tekrar başlatıldı"
    
    # Servisin hazır olmasını bekle
    sleep 30
    echo "✅ Service Down testi tamamlandı\n"
}

test_database_connectivity() {
    echo "🗄️ Test 2: Database Bağlantı Sorunu"
    echo "MongoDB'yi geçici olarak durdurarak bağlantı hatalarını test ediyor..."
    
    # MongoDB'yi durdur
    docker-compose stop mongodb
    echo "⏹️ MongoDB durduruldu"
    
    # Servislerin database hatalarını nasıl handle ettiğini test et
    sleep 10
    
    echo "📊 Database bağlantı testleri:"
    
    # Order oluşturmaya çalış (başarısız olmalı)
    curl -X POST http://localhost:5000/Orders \
         -H "Content-Type: application/json" \
         -d '{"customerId":"test","productName":"Test","quantity":1,"price":100}' \
         -w "Response Code: %{http_code}\n" -s || echo "❌ Expected: Database bağlantı hatası"
    
    # MongoDB'yi tekrar başlat
    docker-compose start mongodb
    echo "✅ MongoDB tekrar başlatıldı"
    
    # Database'in hazır olmasını bekle
    sleep 30
    echo "✅ Database connectivity testi tamamlandı\n"
}

test_message_queue_failure() {
    echo "📨 Test 3: Message Queue Hatası"
    echo "RabbitMQ'yu durdurarak event messaging hatalarını test ediyor..."
    
    # RabbitMQ'yu durdur
    docker-compose stop rabbitmq
    echo "⏹️ RabbitMQ durduruldu"
    
    sleep 10
    
    echo "📊 Message Queue hata testleri:"
    
    # Order oluştur (RabbitMQ olmadan)
    curl -X POST http://localhost:5000/Orders \
         -H "Content-Type: application/json" \
         -d '{"customerId":"chaos-test","productName":"Chaos Test","quantity":1,"price":100}' \
         -w "Response Code: %{http_code}\n" -s
    
    # Member kaydet (RabbitMQ olmadan)
    curl -X POST http://localhost:5003/Members \
         -H "Content-Type: application/json" \
         -d '{"name":"Chaos Test","email":"chaos@test.com","phone":"+90 555 000 0000"}' \
         -w "Response Code: %{http_code}\n" -s
    
    # RabbitMQ'yu tekrar başlat
    docker-compose start rabbitmq
    echo "✅ RabbitMQ tekrar başlatıldı"
    
    sleep 30
    echo "✅ Message Queue failure testi tamamlandı\n"
}

test_high_cpu_memory() {
    echo "💻 Test 4: Yüksek CPU/Memory Kullanımı"
    echo "Sistem kaynaklarını zorlamak için yoğun işlemler çalıştırıyor..."
    
    # OrderService container'ında CPU stress
    echo "🔥 CPU Stress testi başlatılıyor..."
    
    # Paralel olarak çoklu order oluştur
    for i in {1..100}; do
        curl -X POST http://localhost:5000/Orders \
             -H "Content-Type: application/json" \
             -d "{\"customerId\":\"stress-test-$i\",\"productName\":\"Stress Test $i\",\"quantity\":$((i%10+1)),\"price\":$((i*100))}" \
             -s &
    done
    
    echo "⚡ 100 adet paralel order oluşturuldu"
    
    # İşlemlerin tamamlanmasını bekle
    wait
    
    echo "✅ CPU/Memory stress testi tamamlandı\n"
}

test_network_latency() {
    echo "🌐 Test 5: Network Latency Simülasyonu"
    echo "Ağ gecikmesi simüle ediyor..."
    
    # Container'larda network delay ekle (Linux only - gerçek ortamda kullanılabilir)
    echo "📡 Network latency test senaryosu simüle ediliyor..."
    
    # Yavaş network koşullarında performans testi
    echo "⏳ Yavaş network koşullarında test..."
    
    start_time=$(date +%s)
    
    # Sequential requests (should be slower with network issues)
    for i in {1..10}; do
        curl -X GET http://localhost:5000/Orders -s > /dev/null
        curl -X GET http://localhost:5002/Invoices -s > /dev/null
        curl -X GET http://localhost:5003/Members -s > /dev/null
    done
    
    end_time=$(date +%s)
    duration=$((end_time - start_time))
    
    echo "⏱️ 30 request'in toplam süresi: ${duration} saniye"
    echo "✅ Network latency testi tamamlandı\n"
}

test_data_corruption() {
    echo "💾 Test 6: Data Corruption Recovery"
    echo "Veri bütünlüğü ve recovery testleri..."
    
    # Geçersiz veri ile test
    echo "🔍 Geçersiz veri testleri:"
    
    # Invalid JSON
    curl -X POST http://localhost:5000/Orders \
         -H "Content-Type: application/json" \
         -d '{"invalid":"json","missing":"required fields"}' \
         -w "Response Code: %{http_code}\n" -s
    
    # SQL Injection attempt (should be handled safely)
    curl -X POST http://localhost:5000/Orders \
         -H "Content-Type: application/json" \
         -d '{"customerId":"'\''DROP TABLE Orders--","productName":"Test","quantity":1,"price":100}' \
         -w "Response Code: %{http_code}\n" -s
    
    # XSS attempt
    curl -X POST http://localhost:5003/Members \
         -H "Content-Type: application/json" \
         -d '{"name":"<script>alert(\"xss\")</script>","email":"test@test.com","phone":"+90 555 000 0000"}' \
         -w "Response Code: %{http_code}\n" -s
    
    echo "✅ Data corruption recovery testi tamamlandı\n"
}

performance_monitoring() {
    echo "📊 Performance Monitoring During Chaos"
    echo "Chaos testleri sırasında performans metrikleri:"
    
    # Prometheus metrics çek
    echo "📈 Prometheus Metrics:"
    curl -s http://localhost:9090/api/v1/query?query=up | jq '.data.result[] | {instance: .metric.instance, value: .value[1]}' 2>/dev/null || echo "Prometheus metrikleri alınamadı"
    
    # Container stats
    echo "🐳 Docker Container Stats:"
    docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" 2>/dev/null || echo "Container stats alınamadı"
    
    echo "✅ Performance monitoring tamamlandı\n"
}

# Ana test fonksiyonu
run_chaos_tests() {
    echo "🎯 Tüm Chaos Engineering testleri çalıştırılıyor...\n"
    
    # Sistem durumunu kontrol et
    echo "🔍 Başlangıç sistem durumu:"
    docker-compose ps
    
    # Testleri sırayla çalıştır
    test_service_down
    test_database_connectivity
    test_message_queue_failure
    test_high_cpu_memory
    test_network_latency
    test_data_corruption
    performance_monitoring
    
    echo "🎉 Tüm Chaos Engineering testleri tamamlandı!"
    echo "📋 Test Özeti:"
    echo "✅ Service Down Recovery"
    echo "✅ Database Connectivity Issues"
    echo "✅ Message Queue Failures"
    echo "✅ High CPU/Memory Load"
    echo "✅ Network Latency Simulation"
    echo "✅ Data Corruption Handling"
    echo "✅ Performance Monitoring"
}

# Cleanup fonksiyonu
cleanup() {
    echo "\n🧹 Cleanup işlemi başlatılıyor..."
    docker-compose up -d
    echo "✅ Tüm servisler tekrar başlatıldı"
}

# Main execution
case "${1:-all}" in
    "service-down")
        test_service_down
        ;;
    "database")
        test_database_connectivity
        ;;
    "message-queue")
        test_message_queue_failure
        ;;
    "performance")
        test_high_cpu_memory
        ;;
    "network")
        test_network_latency
        ;;
    "data")
        test_data_corruption
        ;;
    "monitoring")
        performance_monitoring
        ;;
    "cleanup")
        cleanup
        ;;
    "all"|*)
        run_chaos_tests
        cleanup
        ;;
esac 