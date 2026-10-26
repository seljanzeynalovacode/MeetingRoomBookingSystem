/* ============================================================
   MRBSystem - Demo seed data (v2)
   Binalar, mərtəbələr, FƏRQLİ ADLI otaqlar, avadanlıqlar və
   DAHA ÇOX bloklama (xüsusən cari olanlar).
   Tarixlər UTC-dir (SYSUTCDATETIME).

   DİQQƏT: Script əvvəlcə mövcud kataloq məlumatını (binalar,
   mərtəbələr, otaqlar, avadanlıqlar, bloklamalar) VƏ otaqlara
   bağlı rezervasiya/iştirakçıları SİLİR, sonra təmiz seed edir.
   Qlobal SystemSettings sətri saxlanılır.

   İşə salma (Docker SQL konteyneri) - istəyə görə:
     docker exec -i mrb-sqlserver /opt/mssql-tools18/bin/sqlcmd \
       -S localhost -U sa -P "YOUR_DB_PASSWORD" -C -d MRBSystemDb \
       < scripts/seed-demo-data.sql
   ============================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ---------- 1) Köhnə məlumatların təmizlənməsi (FK-təhlükəsiz sıra) ---------- */
DELETE FROM RoomEquipment;
DELETE FROM RoomBlackouts;
DELETE FROM Participants;
DELETE FROM Reservations;
DELETE FROM Rooms;
DELETE FROM Floors;
DELETE FROM BuildingWorkingHours;
DELETE FROM SystemSettings WHERE BuildingId IS NOT NULL;
DELETE FROM Buildings;
DELETE FROM Equipment;

DECLARE @now datetime2 = SYSUTCDATETIME();

/* ---------- 2) Avadanlıq kataloqu ---------- */
DECLARE @Equip TABLE (Code varchar(30), Id uniqueidentifier, Name nvarchar(150));
INSERT INTO @Equip (Code, Name) VALUES
 ('PROJ',  N'Projektor'),
 ('WB',    N'Ağ lövhə'),
 ('TV',    N'TV Ekran'),
 ('VC',    N'Video konfrans sistemi'),
 ('PHONE', N'Konfrans telefonu'),
 ('FLIP',  N'Flipchart');
UPDATE @Equip SET Id = NEWID();

INSERT INTO Equipment (Id, Name, CreatedAt, UpdatedAt)
SELECT Id, Name, @now, NULL FROM @Equip;

/* ---------- 3) Binalar (6) və mərtəbə sayı (2-4) ---------- */
DECLARE @Bld TABLE (Code varchar(20), Id uniqueidentifier, Name nvarchar(200), Address nvarchar(300), FloorsCount int);
INSERT INTO @Bld (Code, Name, Address, FloorsCount) VALUES
 ('B1', N'Mərkəzi Ofis',        N'Bakı, Nizami küç. 10',        4),
 ('B2', N'Texnologiya Mərkəzi', N'Bakı, Xətai pr. 25',          3),
 ('B3', N'İnnovasiya Binası',   N'Bakı, Həsən Əliyev küç. 5',   4),
 ('B4', N'Biznes Qüllə',        N'Bakı, Neftçilər pr. 110',     2),
 ('B5', N'Təlim Kampusu',       N'Sumqayıt, Sülh küç. 3',       3),
 ('B6', N'Filial Ofis',         N'Gəncə, Atatürk pr. 40',       2);
UPDATE @Bld SET Id = NEWID();

INSERT INTO Buildings (Id, Name, Address, CreatedAt, UpdatedAt)
SELECT Id, Name, Address, @now, NULL FROM @Bld;

/* ---------- 4) Mərtəbələr ---------- */
DECLARE @Floor TABLE (Id uniqueidentifier, BuildingId uniqueidentifier, BCode varchar(20),
                      Name nvarchar(150), LevelNumber int, RoomsCount int);
INSERT INTO @Floor (BuildingId, BCode, Name, LevelNumber, RoomsCount)
SELECT b.Id, b.Code, CONCAT(n.Lvl, N'-ci mərtəbə'), n.Lvl,
       CASE WHEN n.Lvl IN (1,3) THEN 3 ELSE 2 END      -- tək mərtəbələrdə 3, cütlərdə 2 otaq
FROM @Bld b
JOIN (VALUES (1),(2),(3),(4)) AS n(Lvl) ON n.Lvl <= b.FloorsCount;
UPDATE @Floor SET Id = NEWID();

INSERT INTO Floors (Id, Name, BuildingId, LevelNumber, CreatedAt, UpdatedAt)
SELECT Id, Name, BuildingId, LevelNumber, @now, NULL FROM @Floor;

/* ---------- 5) Otaq adları hovuzu (tematik, fərqli adlar) ---------- */
DECLARE @Names TABLE (Ord int, Nm nvarchar(150));
INSERT INTO @Names (Ord, Nm) VALUES
 (1,N'Araz'),(2,N'Kür'),(3,N'Xəzər'),(4,N'Göygöl'),(5,N'Şahdağ'),(6,N'Zirvə'),
 (7,N'Günəş'),(8,N'Dəniz'),(9,N'Palıd'),(10,N'Çinar'),(11,N'Lalə'),(12,N'Nərgiz'),
 (13,N'Zümrüd'),(14,N'Firuzə'),(15,N'Qartal'),(16,N'Şimşək');

/* ---------- 6) Otaqlar (hər binada ad hovuzundan ardıcıl, unikal) ---------- */
DECLARE @RoomRaw TABLE (FloorId uniqueidentifier, BuildingId uniqueidentifier, BCode varchar(20),
                        Lvl int, Rn int, Capacity int, RnBldg int);
INSERT INTO @RoomRaw (FloorId, BuildingId, BCode, Lvl, Rn, Capacity, RnBldg)
SELECT f.Id, f.BuildingId, f.BCode, f.LevelNumber, r.Rn, 4 + r.Rn * 3,
       ROW_NUMBER() OVER (PARTITION BY f.BuildingId ORDER BY f.LevelNumber, r.Rn)
FROM @Floor f
JOIN (VALUES (1),(2),(3)) AS r(Rn) ON r.Rn <= f.RoomsCount;

DECLARE @Room TABLE (Id uniqueidentifier, FloorId uniqueidentifier, BCode varchar(20),
                     Lvl int, Rn int, Name nvarchar(150), Capacity int);
INSERT INTO @Room (FloorId, BCode, Lvl, Rn, Name, Capacity)
SELECT rr.FloorId, rr.BCode, rr.Lvl, rr.Rn,
       CONCAT(nm.Nm, N' (', rr.BCode, N')'),            -- məs: "Xəzər (B1)" - bina üzrə fərqli
       rr.Capacity
FROM @RoomRaw rr
JOIN @Names nm ON nm.Ord = rr.RnBldg;
UPDATE @Room SET Id = NEWID();

INSERT INTO Rooms (Id, Name, FloorId, Capacity, IsActive, CreatedAt, UpdatedAt)
SELECT Id, Name, FloorId, Capacity, 1, @now, NULL FROM @Room;

/* ---------- 7) Otaq-avadanlıq təyinatı (unikal) ---------- */
DECLARE @RoomEq TABLE (RoomId uniqueidentifier, ECode varchar(30), Qty int);
INSERT INTO @RoomEq SELECT Id, 'WB',    1 FROM @Room;                       -- hər otaqda ağ lövhə
INSERT INTO @RoomEq SELECT Id, 'PROJ',  1 FROM @Room WHERE Rn % 2 = 0;      -- cüt otaqlarda projektor
INSERT INTO @RoomEq SELECT Id, 'TV',    1 FROM @Room WHERE Rn % 2 = 1;      -- tək otaqlarda TV
INSERT INTO @RoomEq SELECT Id, 'VC',    1 FROM @Room WHERE Rn = 3;          -- 3-cü otaqda video konfrans
INSERT INTO @RoomEq SELECT Id, 'PHONE', 1 FROM @Room WHERE Capacity >= 13;  -- böyük otaqlarda telefon
INSERT INTO @RoomEq SELECT Id, 'FLIP',  2 FROM @Room WHERE Capacity >= 13;  -- böyük otaqlarda 2 flipchart

INSERT INTO RoomEquipment (Id, RoomId, EquipmentId, Quantity, CreatedAt, UpdatedAt)
SELECT NEWID(), req.RoomId, e.Id, req.Qty, @now, NULL
FROM @RoomEq req
JOIN @Equip e ON e.Code = req.ECode;

/* ---------- 8) Bloklamalar (16 ədəd: 4 keçmiş, 6 CARİ, 6 gələcək) ---------- */
DECLARE @RN TABLE (Seq int, Id uniqueidentifier);
INSERT INTO @RN (Seq, Id)
SELECT ROW_NUMBER() OVER (ORDER BY BCode, Lvl, Rn), Id FROM @Room;

DECLARE @Black TABLE (Seq int, StartDate datetime2, EndDate datetime2, Reason nvarchar(500));
INSERT INTO @Black (Seq, StartDate, EndDate, Reason) VALUES
 -- KEÇMİŞ (4)
 (1,  DATEADD(DAY,  -15, @now), DATEADD(DAY,  -14, @now),                        N'Köçürülmə işləri (tamamlanıb)'),
 (19, DATEADD(DAY,  -10, @now), DATEADD(DAY,   -7, @now),                        N'Əsaslı təmir (keçmiş)'),
 (34, DATEADD(DAY,   -3, @now), DATEADD(HOUR,   2, DATEADD(DAY,  -3, @now)),      N'Qısa təmir (keçmiş)'),
 (42, DATEADD(DAY,   -1, @now), DATEADD(HOUR,   4, DATEADD(DAY,  -1, @now)),      N'Təmizlik (keçmiş)'),
 -- CARİ / indi aktiv (6)
 (5,  DATEADD(HOUR,   -1, @now), DATEADD(HOUR,   1, @now),                        N'Təmizlik işləri (davam edir)'),
 (13, DATEADD(MINUTE, -30, @now), DATEADD(MINUTE, 90, @now),                      N'Qısa texniki yoxlama'),
 (22, DATEADD(HOUR,   -3, @now), DATEADD(HOUR,   3, @now),                        N'İclas gedir — rezerv'),
 (29, DATEADD(DAY,    -1, @now), DATEADD(DAY,    1, @now),                        N'Texniki xidmət (davam edir)'),
 (37, DATEADD(DAY,    -2, @now), DATEADD(DAY,    3, @now),                        N'Əsaslı təmir (davam edir)'),
 (44, DATEADD(HOUR,   -4, @now), DATEADD(HOUR,  20, @now),                        N'Tədbir hazırlığı'),
 -- GƏLƏCƏK (6)
 (8,  DATEADD(DAY,     1, @now), DATEADD(HOUR,   1, DATEADD(DAY,  1, @now)),       N'Avadanlıq quraşdırılması'),
 (16, DATEADD(DAY,     2, @now), DATEADD(HOUR,   2, DATEADD(DAY,  2, @now)),       N'Prezentasiya hazırlığı'),
 (25, DATEADD(DAY,     3, @now), DATEADD(HOUR,   4, DATEADD(DAY,  3, @now)),       N'Müsahibə otağı rezervi'),
 (31, DATEADD(DAY,     5, @now), DATEADD(DAY,    8, @now),                        N'Konfrans üçün rezerv'),
 (40, DATEADD(DAY,     7, @now), DATEADD(HOUR,   3, DATEADD(DAY,  7, @now)),       N'Təlim sessiyası'),
 (46, DATEADD(DAY,    10, @now), DATEADD(DAY,   11, @now),                        N'Audit üçün bağlı');

INSERT INTO RoomBlackouts (Id, RoomId, StartDate, EndDate, Reason, CreatedAt, UpdatedAt)
SELECT NEWID(), rn.Id, bl.StartDate, bl.EndDate, bl.Reason, @now, NULL
FROM @Black bl
JOIN @RN rn ON rn.Seq = bl.Seq;

COMMIT TRANSACTION;

/* ---------- Nəticə ---------- */
PRINT '--- Seed (v2) tamamlandi ---';
SELECT 'Buildings' AS [Table], COUNT(*) AS Cnt FROM Buildings
UNION ALL SELECT 'Floors',        COUNT(*) FROM Floors
UNION ALL SELECT 'Rooms',         COUNT(*) FROM Rooms
UNION ALL SELECT 'Equipment',     COUNT(*) FROM Equipment
UNION ALL SELECT 'RoomEquipment', COUNT(*) FROM RoomEquipment
UNION ALL SELECT 'RoomBlackouts', COUNT(*) FROM RoomBlackouts;

SELECT
  CASE WHEN SYSUTCDATETIME() BETWEEN StartDate AND EndDate THEN 'CARI'
       WHEN EndDate < SYSUTCDATETIME() THEN 'KECMIS' ELSE 'GELECEK' END AS Status,
  COUNT(*) AS Cnt
FROM RoomBlackouts
GROUP BY CASE WHEN SYSUTCDATETIME() BETWEEN StartDate AND EndDate THEN 'CARI'
              WHEN EndDate < SYSUTCDATETIME() THEN 'KECMIS' ELSE 'GELECEK' END;
