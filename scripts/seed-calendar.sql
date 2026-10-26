/* ============================================================
   MRBSystem - Tam CALENDAR seed
   Hər bina × hər həftə 7 rezervasiya (günə 1-2, fərqli saatlar)
   + hər bina × hər həftə 1 ədəd 1-saatlıq bloklama.
   Həftələr: keçən həftə, bu həftə, növbəti 3 həftə (≈1 ay) = 5 həftə.
   Rezervasiyalar mövcud istifadəçilər arasında növbə ilə paylanır.
   Saatlar Bakı yerli vaxtı; bazaya UTC (−4 saat) saxlanılır.

   DİQQƏT (idempotent): əvvəlcə BÜTÜN rezervasiyalar və bloklamalar
   silinir, sonra bu dəst yaradılır. Otaqlar mövcud olmalıdır.

   İşə salma (Docker):
     docker exec -i mrb-sqlserver /opt/mssql-tools18/bin/sqlcmd \
       -S localhost -U sa -P "YOUR_DB_PASSWORD" -C -d MRBSystemDb \
       < scripts/seed-calendar.sql
   ============================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM Rooms)
BEGIN RAISERROR(N'Otaq yoxdur. Evvelce scripts/seed-demo-data.sql run edin.', 16, 1); RETURN; END
IF NOT EXISTS (SELECT 1 FROM AspNetUsers)
BEGIN RAISERROR(N'Istifadeci yoxdur. Evvelce en azi bir istifadeci qeydiyyatdan kecsin.', 16, 1); RETURN; END

-- Bakı vaxtı ilə bu günün yarı gecəsi (UTC+4)
DECLARE @todayBaku datetime2 = CAST(CAST(DATEADD(HOUR, 4, SYSUTCDATETIME()) AS date) AS datetime2);

/* ---------- İstifadəçilər (növbəli təyinat üçün) ---------- */
DECLARE @U TABLE (Seq int, Id uniqueidentifier);
INSERT INTO @U (Seq, Id)
SELECT ROW_NUMBER() OVER (ORDER BY NormalizedEmail), Id FROM AspNetUsers;
DECLARE @Uc int = (SELECT COUNT(*) FROM @U);

/* ---------- Binalar ---------- */
DECLARE @B TABLE (BIdx int, Id uniqueidentifier);
INSERT INTO @B (BIdx, Id)
SELECT ROW_NUMBER() OVER (ORDER BY Name), Id FROM Buildings;

/* ---------- Bina üzrə otaqlar (təsadüfi sıra) + say ---------- */
DECLARE @RB TABLE (BuildingId uniqueidentifier, RSeq int, RoomId uniqueidentifier);
INSERT INTO @RB (BuildingId, RSeq, RoomId)
SELECT f.BuildingId, ROW_NUMBER() OVER (PARTITION BY f.BuildingId ORDER BY NEWID()), r.Id
FROM Rooms r
JOIN Floors f ON f.Id = r.FloorId;

DECLARE @RBc TABLE (BuildingId uniqueidentifier, Cnt int);
INSERT INTO @RBc SELECT BuildingId, COUNT(*) FROM @RB GROUP BY BuildingId;

/* ---------- Həftələr (keçən, bu, +3) ---------- */
DECLARE @Week TABLE (WkIdx int, WkOffset int);
INSERT INTO @Week (WkIdx, WkOffset) VALUES (1,-1),(2,0),(3,1),(4,2),(5,3);

/* ---------- Həftəlik 7 slot (gün içi fərqli saatlar) ---------- */
DECLARE @Slot TABLE (SlotNo int, Dow int, Hh int, Mi int, DurMin int, Title nvarchar(200));
INSERT INTO @Slot (SlotNo, Dow, Hh, Mi, DurMin, Title) VALUES
 (1, 0,  9,  0,  60, N'Komanda görüşü'),      -- B.e. 09:00
 (2, 0, 14,  0,  60, N'Satış zəngi'),         -- B.e. 14:00
 (3, 1, 11,  0,  90, N'Sprint planlaşdırma'), -- Ç.a. 11:00
 (4, 2, 10,  0,  60, N'Həftəlik sinxron'),    -- Ç. 10:00
 (5, 2, 15, 30,  60, N'Dizayn müzakirəsi'),   -- Ç. 15:30
 (6, 3, 13,  0, 120, N'Məhsul demo'),         -- C.a. 13:00
 (7, 4, 16,  0,  60, N'Retrospektiv');        -- C. 16:00

BEGIN TRANSACTION;

/* ---------- Köhnə calendar məlumatını təmizləyirik ---------- */
DELETE FROM Reservations;   -- Participant-lar FK Cascade ilə silinir
DELETE FROM RoomBlackouts;

/* ---------- Rezervasiyalar (bina × həftə × 7 slot = 210) ---------- */
;WITH gen AS (
    SELECT b.BIdx, b.Id AS BuildingId, w.WkOffset,
           s.Dow, s.Hh, s.Mi, s.DurMin, s.Title, s.SlotNo,
           (w.WkOffset * 7 + s.Dow) AS DayOffset,
           ROW_NUMBER() OVER (PARTITION BY b.Id ORDER BY w.WkOffset, s.SlotNo) AS BOrd,
           ROW_NUMBER() OVER (ORDER BY b.BIdx, w.WkOffset, s.SlotNo)            AS GOrd
    FROM @B b
    CROSS JOIN @Week w
    CROSS JOIN @Slot s
)
INSERT INTO Reservations (Id, Title, StartDate, EndDate, Status, ReservedByUserId, RoomId, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    g.Title,
    x.StartUtc,
    DATEADD(MINUTE, g.DurMin, x.StartUtc),
    'Confirmed',
    u.Id,
    rb.RoomId,
    SYSUTCDATETIME(),
    NULL
FROM gen g
JOIN @RBc c  ON c.BuildingId = g.BuildingId
JOIN @RB  rb ON rb.BuildingId = g.BuildingId AND rb.RSeq = ((g.BOrd - 1) % c.Cnt) + 1
JOIN @U   u  ON u.Seq = ((g.GOrd - 1) % @Uc) + 1
CROSS APPLY (
    SELECT DATEADD(HOUR, -4,
             DATEADD(MINUTE, g.Mi,
             DATEADD(HOUR,   g.Hh + ((g.BIdx - 1) % 3),   -- bina üzrə saat sürüşməsi (müxtəliflik)
             DATEADD(DAY,    g.DayOffset, @todayBaku)))) AS StartUtc
) x;

/* ---------- Bloklamalar (bina × həftə = 30, 1-saatlıq) ---------- */
-- Çərşənbə axşamı 16:00-17:00 (rezervasiya slotları ilə üst-üstə düşmür)
;WITH bgen AS (
    SELECT b.Id AS BuildingId, w.WkOffset,
           ROW_NUMBER() OVER (PARTITION BY b.Id ORDER BY w.WkOffset) AS WOrd
    FROM @B b
    CROSS JOIN @Week w
)
INSERT INTO RoomBlackouts (Id, RoomId, StartDate, EndDate, Reason, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    rb.RoomId,
    x.StartUtc,
    DATEADD(HOUR, 1, x.StartUtc),
    N'Həftəlik texniki baxış',
    SYSUTCDATETIME(),
    NULL
FROM bgen g
JOIN @RBc c  ON c.BuildingId = g.BuildingId
JOIN @RB  rb ON rb.BuildingId = g.BuildingId AND rb.RSeq = ((g.WOrd - 1) % c.Cnt) + 1
CROSS APPLY (
    SELECT DATEADD(HOUR, -4,
             DATEADD(HOUR, 16,
             DATEADD(DAY,  g.WkOffset * 7 + 1, @todayBaku))) AS StartUtc
) x;

COMMIT TRANSACTION;

/* ---------- Nəticə ---------- */
PRINT '--- Calendar seed tamamlandi ---';
SELECT 'Reservations' AS [Table], COUNT(*) AS Cnt FROM Reservations
UNION ALL SELECT 'RoomBlackouts', COUNT(*) FROM RoomBlackouts;

-- Bina üzrə rezervasiya sayı
SELECT b.Name AS Bina, COUNT(*) AS RezervSayi
FROM Reservations res
JOIN Rooms rm ON rm.Id = res.RoomId
JOIN Floors f ON f.Id = rm.FloorId
JOIN Buildings b ON b.Id = f.BuildingId
GROUP BY b.Name
ORDER BY b.Name;

-- Keçmiş / cari / gələcək bölgü
SELECT
    CASE WHEN EndDate < SYSUTCDATETIME() THEN 'KECMIS'
         WHEN SYSUTCDATETIME() BETWEEN StartDate AND EndDate THEN 'CARI'
         ELSE 'GELECEK' END AS Status,
    COUNT(*) AS Cnt
FROM Reservations
GROUP BY CASE WHEN EndDate < SYSUTCDATETIME() THEN 'KECMIS'
              WHEN SYSUTCDATETIME() BETWEEN StartDate AND EndDate THEN 'CARI'
              ELSE 'GELECEK' END;
