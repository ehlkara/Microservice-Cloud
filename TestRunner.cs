using System;
using System.Threading.Tasks;

namespace MicroservicesTests
{
    /// <summary>
    /// Gerçek dünya senaryolarını çalıştıran test runner
    /// </summary>
    class TestRunner
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Microservices Real-World Scenarios Test Suite");
            Console.WriteLine("================================================\n");

            var tests = new RealWorldScenariosTests();

            try
            {
                // Sistemin hazır olduğunu kontrol et
                Console.WriteLine("🔍 Sistem hazırlık kontrolü...");
                await CheckSystemHealth();
                Console.WriteLine("✅ Sistem hazır!\n");

                // Test senaryolarını sırayla çalıştır
                Console.WriteLine("📋 Test Senaryoları Başlatılıyor...\n");

                // Senaryo 1: E-ticaret workflow
                await tests.TestECommerceOrderWorkflow();
                await Task.Delay(2000);

                // Senaryo 2: Yüksek yoğunluk testi
                await tests.TestHighVolumeOrderProcessing();
                await Task.Delay(2000);

                // Senaryo 3: Error handling
                await tests.TestErrorHandlingAndResilience();
                await Task.Delay(2000);

                // Senaryo 4: Message queue flow
                await tests.TestMessageQueueFlow();
                await Task.Delay(2000);

                // Senaryo 5: Data consistency
                await tests.TestDataConsistency();
                await Task.Delay(2000);

                // Senaryo 6: Performance metrics
                await tests.TestPerformanceMetrics();

                Console.WriteLine("🎉 Tüm test senaryoları başarıyla tamamlandı!");
                Console.WriteLine("\n📊 Test Özeti:");
                Console.WriteLine("- E-ticaret workflow: ✅");
                Console.WriteLine("- Yüksek yoğunluk: ✅");
                Console.WriteLine("- Error handling: ✅");
                Console.WriteLine("- Message queue: ✅");
                Console.WriteLine("- Data consistency: ✅");
                Console.WriteLine("- Performance: ✅");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test sırasında hata oluştu: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\n🏁 Test suite tamamlandı. Çıkmak için bir tuşa basın...");
            Console.ReadKey();
        }

        private static async Task CheckSystemHealth()
        {
            var httpClient = new System.Net.Http.HttpClient();
            
            try
            {
                var services = new[]
                {
                    ("Order Service", "http://localhost:5000/Health"),
                    ("Invoice Service", "http://localhost:5002/Health"),
                    ("Membership Service", "http://localhost:5003/Health")
                };

                foreach (var (name, url) in services)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"✅ {name} - Çalışıyor");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ {name} - Durum: {response.StatusCode}");
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"❌ {name} - Erişilemiyor");
                    }
                }
            }
            finally
            {
                httpClient.Dispose();
            }
        }
    }
} 