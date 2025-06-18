# PowerShell Test Runner for Real-World Scenarios
# Bu script Windows'ta test senaryolarını kolayca çalıştırmanızı sağlar

Write-Host "🚀 Microservices Real-World Test Suite" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Test seçenekleri
$testOptions = @{
    "1" = "C# Integration Tests"
    "2" = "K6 Load Testing"
    "3" = "Chaos Engineering"
    "4" = "Full Test Suite"
    "5" = "Docker Environment Setup"
    "6" = "Health Check"
    "7" = "Clean & Reset"
}

function Show-Menu {
    Write-Host "`n📋 Test Seçenekleri:" -ForegroundColor Cyan
    foreach ($key in $testOptions.Keys | Sort-Object) {
        Write-Host "  $key. $($testOptions[$key])" -ForegroundColor White
    }
    Write-Host "  q. Çıkış" -ForegroundColor Yellow
}

function Test-Prerequisites {
    Write-Host "🔍 Ön koşullar kontrol ediliyor..." -ForegroundColor Yellow
    
    # Docker kontrol
    try {
        docker --version | Out-Null
        Write-Host "✅ Docker mevcut" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Docker bulunamadı! Lütfen Docker'ı yükleyin." -ForegroundColor Red
        return $false
    }
    
    # Docker Compose kontrol
    try {
        docker-compose --version | Out-Null
        Write-Host "✅ Docker Compose mevcut" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Docker Compose bulunamadı!" -ForegroundColor Red
        return $false
    }
    
    # .NET SDK kontrol
    try {
        dotnet --version | Out-Null
        Write-Host "✅ .NET SDK mevcut" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ .NET SDK bulunamadı - C# testleri çalışmayabilir" -ForegroundColor Yellow
    }
    
    return $true
}

function Start-DockerEnvironment {
    Write-Host "🐳 Docker environment başlatılıyor..." -ForegroundColor Blue
    
    try {
        docker-compose up -d
        Write-Host "✅ Docker services başlatıldı" -ForegroundColor Green
        
        Write-Host "⏳ Servislerin hazır olması bekleniyor (30 saniye)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 30
        
        return $true
    }
    catch {
        Write-Host "❌ Docker environment başlatılamadı" -ForegroundColor Red
        return $false
    }
}

function Test-ServiceHealth {
    Write-Host "🏥 Servis sağlığı kontrol ediliyor..." -ForegroundColor Blue
    
    $services = @(
        @{Name="Order Service"; Url="http://localhost:5000/Health"},
        @{Name="Invoice Service"; Url="http://localhost:5002/Health"},
        @{Name="Membership Service"; Url="http://localhost:5003/Health"}
    )
    
    $allHealthy = $true
    
    foreach ($service in $services) {
        try {
            $response = Invoke-WebRequest -Uri $service.Url -Method GET -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
                Write-Host "✅ $($service.Name) - Sağlıklı" -ForegroundColor Green
            }
            else {
                Write-Host "⚠️ $($service.Name) - Durum: $($response.StatusCode)" -ForegroundColor Yellow
                $allHealthy = $false
            }
        }
        catch {
            Write-Host "❌ $($service.Name) - Erişilemiyor" -ForegroundColor Red
            $allHealthy = $false
        }
    }
    
    return $allHealthy
}

function Run-CSharpTests {
    Write-Host "🔧 C# Integration Tests çalıştırılıyor..." -ForegroundColor Blue
    
    try {
        # TestRunner.cs dosyasını compile et ve çalıştır
        Write-Host "📦 Test dosyaları compile ediliyor..." -ForegroundColor Yellow
        
        # Geçici proje dosyası oluştur
        $tempProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="6.0.0" />
  </ItemGroup>
</Project>
"@
        
        $tempProject | Out-File -FilePath "TempTestProject.csproj" -Encoding UTF8
        
        dotnet run --project TempTestProject.csproj
        
        # Geçici dosyaları temizle
        Remove-Item "TempTestProject.csproj" -Force -ErrorAction SilentlyContinue
        
        Write-Host "✅ C# testleri tamamlandı" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ C# testleri başarısız: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Run-K6LoadTests {
    Write-Host "⚡ K6 Load Tests çalıştırılıyor..." -ForegroundColor Blue
    
    # K6 kontrol
    try {
        k6 version | Out-Null
        Write-Host "✅ K6 mevcut" -ForegroundColor Green
        
        # K6 testlerini çalıştır
        k6 run real-world-scenarios.js
        
        Write-Host "✅ K6 load testleri tamamlandı" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ K6 bulunamadı. Manuel olarak yükleyebilirsiniz:" -ForegroundColor Yellow
        Write-Host "   winget install k6" -ForegroundColor White
        Write-Host "   veya https://k6.io/docs/getting-started/installation/" -ForegroundColor White
        
        Write-Host "🔄 Alternatif: Basit HTTP testleri çalıştırılıyor..." -ForegroundColor Blue
        Run-SimpleHttpTests
    }
}

function Run-SimpleHttpTests {
    Write-Host "🌐 Basit HTTP performans testleri..." -ForegroundColor Blue
    
    $testData = @(
        @{Name="Üye Kaydı"; Method="POST"; Url="http://localhost:5003/Members"; Body='{"name":"Test User","email":"test@email.com","phone":"+90 555 000 0000"}'},
        @{Name="Sipariş Oluşturma"; Method="POST"; Url="http://localhost:5000/Orders"; Body='{"customerId":"test-customer","productName":"Test Product","quantity":1,"price":100}'},
        @{Name="Siparişleri Listeleme"; Method="GET"; Url="http://localhost:5000/Orders"; Body=""},
        @{Name="Faturaları Listeleme"; Method="GET"; Url="http://localhost:5002/Invoices"; Body=""}
    )
    
    foreach ($test in $testData) {
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            
            if ($test.Method -eq "POST") {
                $response = Invoke-WebRequest -Uri $test.Url -Method POST -Body $test.Body -ContentType "application/json" -TimeoutSec 10
            }
            else {
                $response = Invoke-WebRequest -Uri $test.Url -Method GET -TimeoutSec 10
            }
            
            $stopwatch.Stop()
            
            if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 201) {
                Write-Host "✅ $($test.Name) - $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
            }
            else {
                Write-Host "⚠️ $($test.Name) - Status: $($response.StatusCode)" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "❌ $($test.Name) - Hata: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Start-Sleep -Seconds 1
    }
}

function Run-ChaosTests {
    Write-Host "🔥 Chaos Engineering Tests..." -ForegroundColor Red
    
    if ($IsWindows -or $env:OS -like "Windows*") {
        Write-Host "⚠️ Chaos testleri Linux/Mac'te tam olarak çalışır." -ForegroundColor Yellow
        Write-Host "🔄 Windows için basitleştirilmiş chaos testleri çalıştırılıyor..." -ForegroundColor Blue
        
        # Basit service interruption test
        Write-Host "📊 Service interruption testi..." -ForegroundColor Yellow
        
        try {
            # Bir servisi durdur
            docker-compose stop order-service
            Write-Host "⏹️ OrderService durduruldu" -ForegroundColor Yellow
            
            Start-Sleep -Seconds 10
            
            # Diğer servislerin durumunu kontrol et
            Test-ServiceHealth | Out-Null
            
            # Servisi tekrar başlat
            docker-compose start order-service
            Write-Host "✅ OrderService tekrar başlatıldı" -ForegroundColor Green
            
            Start-Sleep -Seconds 20
        }
        catch {
            Write-Host "❌ Chaos test hatası: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    else {
        # Linux/Mac için tam chaos testleri
        chmod +x chaos-testing.sh
        ./chaos-testing.sh
    }
}

function Clean-Environment {
    Write-Host "🧹 Ortam temizleniyor..." -ForegroundColor Yellow
    
    try {
        docker-compose down
        docker system prune -f
        Write-Host "✅ Docker ortamı temizlendi" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ Temizleme sırasında hata oluştu" -ForegroundColor Yellow
    }
}

# Ana program
function Main {
    if (-not (Test-Prerequisites)) {
        Write-Host "❌ Ön koşullar karşılanmadı. Çıkılıyor..." -ForegroundColor Red
        return
    }
    
    do {
        Show-Menu
        $choice = Read-Host "`n🎯 Seçiminizi yapın"
        
        switch ($choice) {
            "1" {
                if (Test-ServiceHealth) {
                    Run-CSharpTests
                } else {
                    Write-Host "❌ Servisler hazır değil. Önce Docker environment'ı başlatın." -ForegroundColor Red
                }
            }
            "2" { 
                if (Test-ServiceHealth) {
                    Run-K6LoadTests
                } else {
                    Write-Host "❌ Servisler hazır değil. Önce Docker environment'ı başlatın." -ForegroundColor Red
                }
            }
            "3" { 
                Run-ChaosTests
            }
            "4" {
                Write-Host "🚀 Full Test Suite başlatılıyor..." -ForegroundColor Green
                if (-not (Test-ServiceHealth)) {
                    Start-DockerEnvironment | Out-Null
                }
                Run-CSharpTests
                Run-K6LoadTests
                Run-ChaosTests
                Write-Host "🎉 Tüm testler tamamlandı!" -ForegroundColor Green
            }
            "5" { 
                Start-DockerEnvironment | Out-Null
            }
            "6" { 
                Test-ServiceHealth | Out-Null
            }
            "7" { 
                Clean-Environment
            }
            "q" { 
                Write-Host "👋 Görüşmek üzere!" -ForegroundColor Green
                return
            }
            default { 
                Write-Host "❌ Geçersiz seçim!" -ForegroundColor Red
            }
        }
        
        if ($choice -ne "q") {
            Write-Host "`n📝 Devam etmek için Enter'a basın..." -ForegroundColor Cyan
            Read-Host
        }
        
    } while ($choice -ne "q")
}

# Scripti çalıştır
Main 