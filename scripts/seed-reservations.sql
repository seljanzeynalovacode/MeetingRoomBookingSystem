/* ============================================================
   MRBSystem - Demo REZERVASIYALAR
   İstifadəçi: seli.zeynalzade.2006@gmail.com
   Çoxlu gələcək (bu/gələn həftə sıx, 1 aya qədər) + bir qədər keçmiş.
   Fərqli adlar, fərqli saatlar, müxtəlif otaqlar.

   Saatlar Bakı yerli vaxtı kimi yazılıb, bazaya UTC (−4 saat) saxlanılır.
   Təkrar run: əvvəlcə bu istifadəçinin köhnə rezervasiyalarını silir.

   ŞƏRT: bu email qeydiyyatdan keçmiş olmalı və otaqlar mövcud olmalıdır
         (əvvəlcə seed-demo-data.sql run edilməli).

   İşə salma (Docker):
     docker exec -i mrb-sqlserver /opt/mssql-tools18/bin/sqlcmd \
       -S localhost -U sa -P "YOUR_DB_PASSWORD" -C -d MRBSystemDb \
       < scripts/seed-reservations.sql
   ============================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @email nvarchar(256) = N'seli.zeynalzade.2006@gmail.com';
DECLARE @uid uniqueidentifier =
    (SELECT TOP 1 Id FROM AspNetUsers WHERE NormalizedEmail = UPPER(@email));

IF @uid IS NULL
BEGIN
    RAISERROR(N'Istifadeci tapilmadi. Evvelce bu email ile qeydiyyatdan kecin: seli.zeynalzade.2006@gmail.com', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM Rooms)
BEGIN
    RAISERROR(N'Otaq yoxdur. Evvelce scripts/seed-demo-data.sql run edin.', 16, 1);
    RETURN;
END

-- Bakı vaxtı ilə bu günün yarı gecəsi (UTC+4)
DECLARE @todayBaku datetime2 = CAST(CAST(DATEADD(HOUR, 4, SYSUTCDATETIME()) AS date) AS datetime2);

/* ---------- Otaqları təsadüfi sıraya salırıq (müxtəlif otaq/bina) ---------- */
DECLARE @RoomList TABLE (Seq int, Id uniqueidentifier);
INSERT INTO @RoomList (Seq, Id)
SELECT ROW_NUMBER() OVER (ORDER BY NEWID()), Id FROM Rooms;
DECLARE @Rc int = (SELECT COUNT(*) FROM @RoomList);

/* ---------- Rezervasiya siyahısı ---------- */
-- DayOffset: bu gündən fərq (mənfi = keçmiş). Saat/dəqiqə Bakı vaxtı.
DECLARE @Res TABLE (
    RIdx        int IDENTITY(1,1),
    DayOffset   int,
    StartHour   int,
    StartMin    int,
    DurationMin int,
    Title       nvarchar(200),
    Status      nvarchar(20)
);
INSERT INTO @Res (DayOffset, StartHour, StartMin, DurationMin, Title, Status) VALUES
 -- ===== KEÇMİŞ =====
 (-14, 10,  0,  60,  N'Komanda görüşü',        'Confirmed'),
 (-10, 14, 30,  90,  N'Layihə təqdimatı',      'Confirmed'),
 ( -7,  9,  0, 120,  N'Təlim sessiyası',       'Confirmed'),
 ( -5, 16,  0,  60,  N'Müştəri görüşü',        'Cancelled'),
 ( -3, 11,  0,  45,  N'1:1 görüş',             'Confirmed'),
 ( -1, 15,  0,  60,  N'Retrospektiv',          'Confirmed'),
 -- ===== BU HƏFTƏ (sıx) =====
 (  0, 13,  0,  60,  N'Gündəlik standup',      'Confirmed'),
 (  0, 17,  0,  60,  N'QA sinxron',            'Confirmed'),
 (  1, 10,  0,  90,  N'Sprint planlaşdırma',   'Confirmed'),
 (  1, 15, 30,  60,  N'Dizayn müzakirəsi',     'Confirmed'),
 (  2,  9, 30,  60,  N'Həftəlik sinxron',      'Confirmed'),
 (  2, 14,  0, 120,  N'Məhsul demo',           'Confirmed'),
 (  3, 11,  0,  60,  N'Marketinq brifinqi',    'Confirmed'),
 (  4, 16,  0,  90,  N'Strategiya görüşü',     'Confirmed'),
 (  5, 10,  0,  60,  N'Satış zəngi',           'Confirmed'),
 (  6, 13, 30,  60,  N'Texniki müzakirə',      'Confirmed'),
 -- ===== GƏLƏN HƏFTƏ (sıx) =====
 (  7, 10,  0,  60,  N'Komanda görüşü',        'Confirmed'),
 (  8, 14,  0,  90,  N'Büdcə icmalı',          'Confirmed'),
 (  9,  9,  0, 120,  N'Onboarding',            'Confirmed'),
 (  9, 15,  0,  60,  N'Müsahibə',              'Confirmed'),
 ( 10, 11, 30,  60,  N'Roadmap icmalı',        'Confirmed'),
 ( 11, 16, 30,  60,  N'Partnyor görüşü',       'Confirmed'),
 ( 12, 10,  0,  90,  N'İdarə heyəti iclası',   'Confirmed'),
 ( 13, 13,  0,  60,  N'Retrospektiv',          'Confirmed'),
 -- ===== SONRAKI (1 aya qədər, seyrək) =====
 ( 17, 10,  0,  60,  N'Layihə təqdimatı',      'Confirmed'),
 ( 20, 14,  0,  90,  N'Strategiya görüşü',     'Confirmed'),
 ( 24, 11,  0,  60,  N'Müştəri görüşü',        'Confirmed'),
 ( 28, 15,  0,  60,  N'Təlim sessiyası',       'Confirmed'),
 ( 30, 10,  0, 120,  N'Planlaşdırma seminarı', 'Confirmed');

BEGIN TRANSACTION;

-- Təkrar run üçün: bu istifadəçinin köhnə rezervasiyalarını silirik
-- (Participant-lar FK Cascade ilə avtomatik silinir)
DELETE FROM Reservations WHERE ReservedByUserId = @uid;

INSERT INTO Reservations (Id, Title, StartDate, EndDate, Status, ReservedByUserId, RoomId, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    r.Title,
    x.StartUtc,
    DATEADD(MINUTE, r.DurationMin, x.StartUtc),
    r.Status,
    @uid,
    rl.Id,
    SYSUTCDATETIME(),
    NULL
FROM @Res r
JOIN @RoomList rl ON rl.Seq = ((r.RIdx - 1) % @Rc) + 1
CROSS APPLY (
    SELECT DATEADD(HOUR, -4,
             DATEADD(MINUTE, r.StartMin,
             DATEADD(HOUR,   r.StartHour,
             DATEADD(DAY,    r.DayOffset, @todayBaku)))) AS StartUtc
) x;

COMMIT TRANSACTION;

/* ---------- Nəticə ---------- */
PRINT '--- Rezervasiyalar elave olundu ---';
SELECT
    CASE WHEN EndDate < SYSUTCDATETIME() THEN 'KECMIS'
         WHEN SYSUTCDATETIME() BETWEEN StartDate AND EndDate THEN 'CARI'
         ELSE 'GELECEK' END AS Status,
    COUNT(*) AS Cnt
FROM Reservations
WHERE ReservedByUserId = @uid
GROUP BY CASE WHEN EndDate < SYSUTCDATETIME() THEN 'KECMIS'
              WHEN SYSUTCDATETIME() BETWEEN StartDate AND EndDate THEN 'CARI'
              ELSE 'GELECEK' END;

-- Bakı vaxtı ilə ilk 10 yaxın rezervasiya
SELECT TOP 10
    res.Title,
    CONVERT(varchar(16), DATEADD(HOUR, 4, res.StartDate), 120) AS StartBaku,
    CONVERT(varchar(16), DATEADD(HOUR, 4, res.EndDate),   120) AS EndBaku,
    res.Status,
    rm.Name AS Room
FROM Reservations res
JOIN Rooms rm ON rm.Id = res.RoomId
WHERE res.ReservedByUserId = @uid AND res.EndDate >= SYSUTCDATETIME()
ORDER BY res.StartDate;
