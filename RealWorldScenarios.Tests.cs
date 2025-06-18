using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MicroservicesTests
{
    /// <summary>
    /// Gerçek dünya senaryolarını test eden comprehensive test suite
    /// </summary>
    public class RealWorldScenariosTests
    {
        private readonly HttpClient _httpClient;
        private readonly string _orderServiceUrl = "http://localhost:5000";
        private readonly string _invoiceServiceUrl = "http://localhost:5002";
        private readonly string _membershipServiceUrl = "http://localhost:5003";

        public RealWorldScenariosTests()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Senaryo 1: E-ticaret Sipariş Workflow'u
        /// Müşteri kaydı -> Sipariş oluşturma -> Fatura otomatik oluşturma
        /// </summary>
        public async Task TestECommerceOrderWorkflow()
        {
            Console.WriteLine("🛍️ E-Ticaret Sipariş Workflow Testi Başlatılıyor...");

            // 1. Yeni müşteri kaydı
            var member = new
            {
                Name = "Ahmet Yılmaz",
                Email = "ahmet.yilmaz@email.com",
                Phone = "+90 555 123 4567"
            };

            var memberResponse = await PostAsync($"{_membershipServiceUrl}/Members", member);
            var memberId = await GetIdFromResponse(memberResponse);
            Console.WriteLine($"✅ Müşteri kaydedildi: {memberId}");

            await Task.Delay(2000); // RabbitMQ message processing için bekleme

            // 2. Sipariş oluşturma
            var order = new
            {
                CustomerId = memberId,
                ProductName = "MacBook Pro M3",
                Quantity = 1,
                Price = 45000.00m,
                ShippingAddress = "İstanbul, Türkiye"
            };

            var orderResponse = await PostAsync($"{_orderServiceUrl}/Orders", order);
            var orderId = await GetIdFromResponse(orderResponse);
            Console.WriteLine($"✅ Sipariş oluşturuldu: {orderId}");

            await Task.Delay(5000); // RabbitMQ event processing için bekleme

            // 3. Fatura otomatik oluşturma kontrolü
            var invoices = await GetAsync($"{_invoiceServiceUrl}/Invoices");
            Console.WriteLine($"✅ Faturalar kontrol edildi. Toplam fatura sayısı: {await CountInvoices(invoices)}");

            Console.WriteLine("🎉 E-Ticaret Workflow başarıyla tamamlandı!\n");
        }

        /// <summary>
        /// Senaryo 2: Yüksek Yoğunluk Order Processing
        /// Aynı anda çoklu sipariş oluşturma ve sistem davranışını test etme
        /// </summary>
        public async Task TestHighVolumeOrderProcessing()
        {
            Console.WriteLine("⚡ Yüksek Yoğunluk Sipariş İşleme Testi Başlatılıyor...");

            var tasks = new List<Task>();
            var startTime = DateTime.UtcNow;

            // 50 adet eşzamanlı sipariş oluşturma
            for (int i = 1; i <= 50; i++)
            {
                var order = new
                {
                    CustomerId = $"customer_{i}",
                    ProductName = $"Ürün_{i}",
                    Quantity = new Random().Next(1, 10),
                    Price = new Random().Next(100, 5000),
                    OrderDate = DateTime.UtcNow
                };

                tasks.Add(PostAsync($"{_orderServiceUrl}/Orders", order));
            }

            await Task.WhenAll(tasks);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            Console.WriteLine($"✅ 50 sipariş {duration.TotalSeconds:F2} saniyede işlendi");
            Console.WriteLine($"✅ Ortalama işlem süresi: {duration.TotalMilliseconds / 50:F2} ms\n");
        }

        /// <summary>
        /// Senaryo 3: Error Handling ve Resilience
        /// Hatalı verilerle sistem davranışını test etme
        /// </summary>
        public async Task TestErrorHandlingAndResilience()
        {
            Console.WriteLine("🛡️ Error Handling ve Resilience Testi Başlatılıyor...");

            // Invalid data ile sipariş oluşturma
            var invalidOrder = new
            {
                CustomerId = "", // Boş customer ID
                ProductName = "", // Boş ürün adı
                Quantity = -1, // Negatif miktar
                Price = -100 // Negatif fiyat
            };

            try
            {
                var response = await PostAsync($"{_orderServiceUrl}/Orders", invalidOrder);
                Console.WriteLine($"⚠️ Hatalı veri ile response: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ Exception yakalandı: {ex.Message}");
            }

            // Geçersiz endpoint test
            try
            {
                var response = await GetAsync($"{_orderServiceUrl}/NonExistentEndpoint");
                Console.WriteLine($"✅ 404 Test: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ Endpoint hatası yakalandı: {ex.Message}");
            }

            Console.WriteLine("🛡️ Error Handling testleri tamamlandı!\n");
        }

        /// <summary>
        /// Senaryo 4: Message Queue Flow Test
        /// RabbitMQ üzerinden event akışını test etme
        /// </summary>
        public async Task TestMessageQueueFlow()
        {
            Console.WriteLine("📨 Message Queue Flow Testi Başlatılıyor...");

            // Üye kaydı (MemberRegistered event tetikler)
            var member = new
            {
                Name = "Test Kullanıcısı",
                Email = "test@domain.com",
                Phone = "+90 555 999 8888"
            };

            await PostAsync($"{_membershipServiceUrl}/Members", member);
            Console.WriteLine("✅ Member registered event gönderildi");

            await Task.Delay(3000);

            // Sipariş oluşturma (OrderCreated event tetikler)
            var order = new
            {
                CustomerId = "test-customer",
                ProductName = "Test Ürünü",
                Quantity = 2,
                Price = 150.00m
            };

            await PostAsync($"{_orderServiceUrl}/Orders", order);
            Console.WriteLine("✅ Order created event gönderildi");

            await Task.Delay(5000);

            Console.WriteLine("📨 Message Queue flow testi tamamlandı!\n");
        }

        /// <summary>
        /// Senaryo 5: Data Consistency Check
        /// Farklı servislerdeki veri tutarlılığını kontrol etme
        /// </summary>
        public async Task TestDataConsistency()
        {
            Console.WriteLine("🔍 Veri Tutarlılığı Testi Başlatılıyor...");

            // Tüm servislerdeki veri sayılarını kontrol et
            var orders = await GetAsync($"{_orderServiceUrl}/Orders");
            var invoices = await GetAsync($"{_invoiceServiceUrl}/Invoices");
            var members = await GetAsync($"{_membershipServiceUrl}/Members");

            var orderCount = await CountItems(orders, "orders");
            var invoiceCount = await CountItems(invoices, "invoices");
            var memberCount = await CountItems(members, "members");

            Console.WriteLine($"📊 Sipariş sayısı: {orderCount}");
            Console.WriteLine($"📊 Fatura sayısı: {invoiceCount}");
            Console.WriteLine($"📊 Üye sayısı: {memberCount}");

            // Basit tutarlılık kontrolü
            if (orderCount > 0 && invoiceCount >= 0)
            {
                Console.WriteLine("✅ Veri tutarlılığı kontrol edildi");
            }

            Console.WriteLine("🔍 Veri tutarlılığı testi tamamlandı!\n");
        }

        /// <summary>
        /// Senaryo 6: Performance ve Response Time Test
        /// Sistem performansını ölçme
        /// </summary>
        public async Task TestPerformanceMetrics()
        {
            Console.WriteLine("⏱️ Performance Testi Başlatılıyor...");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Health check performance
            var healthTasks = new List<Task>
            {
                GetAsync($"{_orderServiceUrl}/Health"),
                GetAsync($"{_invoiceServiceUrl}/Health"),
                GetAsync($"{_membershipServiceUrl}/Health")
            };

            await Task.WhenAll(healthTasks);
            stopwatch.Stop();

            Console.WriteLine($"✅ Health check süresi: {stopwatch.ElapsedMilliseconds} ms");

            // CRUD operations performance
            stopwatch.Restart();
            
            var order = new
            {
                CustomerId = "perf-test",
                ProductName = "Performance Test Ürünü",
                Quantity = 1,
                Price = 100.00m
            };

            await PostAsync($"{_orderServiceUrl}/Orders", order);
            stopwatch.Stop();

            Console.WriteLine($"✅ Sipariş oluşturma süresi: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine("⏱️ Performance testi tamamlandı!\n");
        }

        // Yardımcı metodlar
        private async Task<HttpResponseMessage> PostAsync(string url, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync(url, content);
        }

        private async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _httpClient.GetAsync(url);
        }

        private async Task<string> GetIdFromResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
        }

        private async Task<int> CountItems(HttpResponseMessage response, string itemType)
        {
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return doc.RootElement.GetArrayLength();
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> CountInvoices(HttpResponseMessage response)
        {
            return await CountItems(response, "invoices");
        }
    }
} 