# CompanySearch

CompanySearch, belirli bir konum ve yaricap icindeki isletmeleri toplayan, web sitelerini analiz eden, her lead icin rapor ureten ve otomatik satis emaili olusturan clean-architecture tabanli bir full-stack uygulamadir.

## Neler var

- `.NET 9 Web API` + `MediatR` + `EF Core` + `PostgreSQL`
- `Redis` cache
- `Hangfire` background jobs
- `React + Vite + TailwindCSS` dashboard
- `Serilog` loglama
- `Docker Compose` ile tek komutluk local ortam
- `OpenStreetMap` uzerinden konum -> lat/lng ve yaricap icindeki isletme toplama
- `OpenAI` destekli email taslagi uretimi
- `MailKit` ile SMTP gonderimi

## Mimari

Kaynak kod `src/` altinda katmanlara ayrildi:

- `CompanySearch.Domain`: entity, enum ve value object'ler
- `CompanySearch.Application`: command/query akislari, DTO'lar, repository/service abstraction'lari
- `CompanySearch.Infrastructure`: EF Core, OSM/OpenAI/SMTP adapter'lari, cache, jobs
- `CompanySearch.Api`: controller'lar, composition root, config, swagger, hangfire dashboard
- `frontend/`: React dashboard

## Is akisi

1. `POST /api/search` yeni bir arama job'u olusturur.
2. Hangfire job'u OSM uzerinden isletmeleri toplar ve duplicate kayitlari engeller.
3. Web sitesi olan kayitlar icin website audit job'u calisir.
4. Audit sonucu skor ve issue listesi olarak kaydedilir.
5. Email generation job'u veya manuel endpoint ile taslak email uretilir.
6. SMTP sandbox veya gercek SMTP ile email gonderilir.

## OSM notu

Overpass API klasik pagination sunmadigi icin arama alanini tile'lara bolen bir strateji kullanildi. Her tile ayrica sorgulaniyor, sonra sonuclar `Source + ExternalId` uzerinden dedupe ediliyor. Bu, genis yaricaplarda kayip yasamadan "tum isletmeler" akisini daha guvenilir hale getirir.

## Kurulum

### 1. Docker ile

```bash
cp .env.example .env
docker compose up --build
```

Ardindan:

- Frontend: `http://localhost:3000`
- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- Hangfire: `http://localhost:5000/hangfire`

### 2. Lokal gelistirme

Backend:

```bash
dotnet restore CompanySearch.sln
dotnet run --project src/CompanySearch.Api/CompanySearch.Api.csproj
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

Vite dev server `api`, `health` ve `hangfire` isteklerini otomatik olarak `http://localhost:5000` adresine proxy eder.

## Config

Temel ayarlar:

- `ConnectionStrings__PostgreSql`
- `ConnectionStrings__Redis`
- `OpenAI__ApiKey`
- `OpenAI__Model`
- `EmailDelivery__SandboxMode`
- `EmailDelivery__Host`
- `EmailDelivery__Username`
- `EmailDelivery__Password`

Varsayilan olarak email gonderimi `SandboxMode=true` gelir. Bu modda gonderim endpoint'i basarili doner ama gercek SMTP teslimi yapmaz.

## API endpointleri

- `POST /api/search`
- `GET /api/businesses`
- `GET /api/businesses/{businessId}`
- `GET /api/businesses/export`
- `POST /api/analyze/{businessId}`
- `POST /api/generate-email/{businessId}`
- `POST /api/send-email/{businessId}`
- `POST /api/emails/bulk-send`

## Frontend ekranlari

- Dashboard: arama konsolu, KPI metrikleri, harita, lead listesi
- Lead detail: analiz ozeti, issue listesi, email onizleme

## Ornek gelistirme notlari

- Demo amacli baslangic verisi seed edilir.
- `OpenAI__ApiKey` yoksa sistem fallback template ile email taslagi uretebilir.
- `WebsiteCrawlerService` ana sayfa bazli teknik/SEO/performance/UX kontrolu yapar.
- `WebsiteAnalysis` ve `Issues` JSONB kolonu olarak saklanir.

## Sonraki iyilestirmeler

- Google Places provider eklemek
- EF migration dosyalari eklemek
- Authentication/authorization eklemek
- Email tracking pixel ve reply webhook akisi eklemek
- Multi-tenant org yapisi eklemek
