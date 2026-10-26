# MeetingRoomBookingSystem — Docker Deployment

## Qurma (Getting Started)

### Tələblər
- Docker & Docker Compose
- SQL Server SA Password: `YourStrongPassword`

### Addımlar

1. **Docker Compose-u Başlat**
   ```bash
   docker compose up -d
   ```

2. **Database Migration (ilk dəfə)**
   ```bash
   docker exec mrb-backend dotnet ef database update \
     --project infrasturucture/MRBSystem.Persistence \
     --startup-project presentation/MRBSystem.WebAPI
   ```

3. **Erişim**
   - Frontend: http://localhost
   - Backend API: http://localhost:8080
   - SQL Server: localhost:1433

### Default Admin Credentials (example)
- Email: `admin@meethub.local`
- Password: `Admin@123456`

---

## Services

| Service | Port | Container |
|---------|------|-----------|
| Frontend | 80 | mrb-frontend |
| Backend API | 8080 | mrb-backend |
| SQL Server | 1433 | mrb-sqlserver |

---

## Environment Variables

`.env` faylında aşağıdakı parametrləri tənzimləyin:

```env
ASPNETCORE_ENVIRONMENT=Production
JWT_KEY=your-secret-key-minimum-32-chars
ADMIN_EMAIL=admin@meethub.local
ADMIN_PASSWORD=Admin@123456
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_EMAIL=your-email@gmail.com
SMTP_PASSWORD=your-app-password
GEMINI_API_KEY=your-gemini-api-key
TIMEZONE=Asia/Baku
```

---

## Logs

Loqları yoxlamaq:
```bash
docker compose logs backend
docker compose logs frontend
docker compose logs sqlserver
```

---

## Stop & Cleanup

```bash
docker compose down                    # Stop containers
docker volume rm meetingroombookingsystem_sqlserver-data   # Delete database
```

---

## Frontend API Configuration

Frontend-in backend ilə əlaqəsini təmin etmək:
- Fayl: `Frontend/javascripts/api/apiClient.js`
- Parametr: `API_BASE_URL = "http://localhost:8080/api"`

---

## Notes

- **Production**: HTTPS sertifikatı əlavə edin (ASP.NET)
- **Database**: Manual migration tələb olunur (ilk başlama)
- **Secrets**: `.env` faylını `.gitignore`-ə əlavə edin
