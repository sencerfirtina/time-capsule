# TimeCapsule API ⏳

TimeCapsule, kullanıcıların hava sıcaklığı, crypto değerleri, tarih, konum ve Spotify hesaplarını bağlayarak kişisel müziklerini tetikleyici olarak içerebilen zaman kapsülleri oluşturmasını sağlayan modern, durumsuz (stateless) bir arka uç (backend) servisidir. 

Proje şu an **Phase 1 (Backend & Altyapı)** aşamasını tamamlamıştır. Kullanıcı arayüzü (UI) geliştirme aşamasındadır.

## 🚀 Öne Çıkan Mühendislik Çözümleri

* **Stateless OAuth2 Akışı:** Geleneksel Session/Cookie tabanlı yetkilendirme yerine, Spotify Callback işlemlerinde güvenliği sağlamak (CSRF koruması) ve durumsuz yapıyı korumak için kısa ömürlü, özel imtiyazlı JWT'ler (`state` parametresi üzerinden) kullanılmıştır.
* **Konteynerizasyon & Çevresel İzolasyon:** Uygulama tamamen Dockerize edilmiş olup, veritabanı bağlantıları ve API anahtarları Environment Variable'lar üzerinden yönetilmektedir.
* **Otomatik Veritabanı Yönetimi:** Uygulama ayağa kalkarken `Entity Framework Core` üzerinden `Database.Migrate()` işlemini otomatik olarak gerçekleştirir.

## 🛠 Tech Stack
* **Framework:** C# / ASP.NET Core Web API
* **Veritabanı:** PostgreSQL & Entity Framework Core
* **Kimlik Doğrulama:** JWT (JSON Web Token), Spotify OAuth2 API
* **Altyapı:** Docker, Render (Cloud Hosting)

## 🚧 Roadmap
- [x] Temel API uç noktalarının oluşturulması
- [x] PostgreSQL entegrasyonu ve Docker imajının hazırlanması
- [x] Render üzerinde CI/CD ve canlıya alım
- [x] Spotify API ile Stateless OAuth2 entegrasyonu
- [ ] **Phase 2:** Retro-Fütüristik (Y2K/Cassette Futurism) temalı React/Tailwind kullanıcı arayüzünün (UI) geliştirilmesi.

## 💡 Canlı Ortam Notu
Bu projenin API ve veritabanı altyapısı Render üzerinde barındırılmaktadır. Platformun ücretsiz katman (free-tier) kısıtlamaları ve süre sınırları nedeniyle canlı sunucu geçici olarak uyku modunda veya çevrimdışı olabilir. Projeyi tam performansla incelemek için aşağıdaki lokal kurulum adımlarını takip edebilirsiniz.

## 💻 Lokalde Çalıştırma
Projeyi kendi bilgisayarınızda çalıştırmak için:
1. Repoyu klonlayın: `git clone https://github.com/sencerfirtina/time-capsule`
2. `appsettings.Development.json` dosyanızı kendi Spotify Client ID, Client Secret ve PostgreSQL verilerinizle doldurun.
3. Docker üzerinden ayağa kaldırın:
   ```bash
   docker-compose up --build
