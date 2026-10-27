/* ============================================================
   MRBSystem - Demo seed data (PostgreSQL)
   Binalar, mərtəbələr, fərqli adlı otaqlar, avadanlıqlar, bloklamalar.
   Tarixlər UTC-dir. Script əvvəlcə mövcud kataloq + asılı
   rezervasiyaları silir, sonra təmiz seed edir.

   İşə salma (Docker):
     docker exec -i mrb-postgres psql -U postgres -d MRBSystemDb \
       < scripts/seed-demo-data.sql
   ============================================================ */

BEGIN;

-- 1) Köhnə məlumatların təmizlənməsi (FK-təhlükəsiz sıra)
DELETE FROM "RoomEquipment";
DELETE FROM "RoomBlackouts";
DELETE FROM "Participants";
DELETE FROM "Reservations";
DELETE FROM "Rooms";
DELETE FROM "Floors";
DELETE FROM "BuildingWorkingHours";
DELETE FROM "SystemSettings" WHERE "BuildingId" IS NOT NULL;
DELETE FROM "Buildings";
DELETE FROM "Equipment";

-- 2) Avadanlıq kataloqu
CREATE TEMP TABLE tmp_equip (code text, id uuid DEFAULT gen_random_uuid(), name text) ON COMMIT DROP;
INSERT INTO tmp_equip (code, name) VALUES
 ('PROJ','Projektor'),('WB','Ağ lövhə'),('TV','TV Ekran'),
 ('VC','Video konfrans sistemi'),('PHONE','Konfrans telefonu'),('FLIP','Flipchart');
INSERT INTO "Equipment" ("Id","Name","CreatedAt","UpdatedAt")
SELECT id, name, (now() AT TIME ZONE 'utc'), NULL FROM tmp_equip;

-- 3) Binalar (6) + mərtəbə sayı (2-4)
CREATE TEMP TABLE tmp_bld (code text, id uuid DEFAULT gen_random_uuid(), name text, address text, floors int) ON COMMIT DROP;
INSERT INTO tmp_bld (code, name, address, floors) VALUES
 ('B1','Mərkəzi Ofis','Bakı, Nizami küç. 10',4),
 ('B2','Texnologiya Mərkəzi','Bakı, Xətai pr. 25',3),
 ('B3','İnnovasiya Binası','Bakı, Həsən Əliyev küç. 5',4),
 ('B4','Biznes Qüllə','Bakı, Neftçilər pr. 110',2),
 ('B5','Təlim Kampusu','Sumqayıt, Sülh küç. 3',3),
 ('B6','Filial Ofis','Gəncə, Atatürk pr. 40',2);
INSERT INTO "Buildings" ("Id","Name","Address","CreatedAt","UpdatedAt")
SELECT id, name, address, (now() AT TIME ZONE 'utc'), NULL FROM tmp_bld;

-- 4) Mərtəbələr
CREATE TEMP TABLE tmp_floor (id uuid DEFAULT gen_random_uuid(), building_id uuid, bcode text, name text, level int, rooms int) ON COMMIT DROP;
INSERT INTO tmp_floor (building_id, bcode, name, level, rooms)
SELECT b.id, b.code, (n.lvl || '-ci mərtəbə'), n.lvl,
       CASE WHEN n.lvl IN (1,3) THEN 3 ELSE 2 END
FROM tmp_bld b
JOIN (VALUES (1),(2),(3),(4)) AS n(lvl) ON n.lvl <= b.floors;
INSERT INTO "Floors" ("Id","Name","BuildingId","LevelNumber","CreatedAt","UpdatedAt")
SELECT id, name, building_id, level, (now() AT TIME ZONE 'utc'), NULL FROM tmp_floor;

-- 5) Otaq adları hovuzu
CREATE TEMP TABLE tmp_names (ord int, nm text) ON COMMIT DROP;
INSERT INTO tmp_names (ord, nm) VALUES
 (1,'Araz'),(2,'Kür'),(3,'Xəzər'),(4,'Göygöl'),(5,'Şahdağ'),(6,'Zirvə'),
 (7,'Günəş'),(8,'Dəniz'),(9,'Palıd'),(10,'Çinar'),(11,'Lalə'),(12,'Nərgiz'),
 (13,'Zümrüd'),(14,'Firuzə'),(15,'Qartal'),(16,'Şimşək');

-- 6) Otaqlar
CREATE TEMP TABLE tmp_room (id uuid DEFAULT gen_random_uuid(), floor_id uuid, bcode text, lvl int, rn int, name text, capacity int) ON COMMIT DROP;
INSERT INTO tmp_room (floor_id, bcode, lvl, rn, name, capacity)
SELECT rr.floor_id, rr.bcode, rr.lvl, rr.rn,
       nm.nm || ' (' || rr.bcode || ')',
       rr.capacity
FROM (
    SELECT f.id AS floor_id, f.bcode, f.level AS lvl, r.rn, 4 + r.rn * 3 AS capacity,
           row_number() OVER (PARTITION BY f.building_id ORDER BY f.level, r.rn) AS rnbldg
    FROM tmp_floor f
    JOIN (VALUES (1),(2),(3)) AS r(rn) ON r.rn <= f.rooms
) rr
JOIN tmp_names nm ON nm.ord = rr.rnbldg;
INSERT INTO "Rooms" ("Id","Name","FloorId","Capacity","IsActive","CreatedAt","UpdatedAt")
SELECT id, name, floor_id, capacity, true, (now() AT TIME ZONE 'utc'), NULL FROM tmp_room;

-- 7) Otaq-avadanlıq təyinatı (unikal)
CREATE TEMP TABLE tmp_re (room_id uuid, ecode text, qty int) ON COMMIT DROP;
INSERT INTO tmp_re SELECT id,'WB',1    FROM tmp_room;
INSERT INTO tmp_re SELECT id,'PROJ',1  FROM tmp_room WHERE rn % 2 = 0;
INSERT INTO tmp_re SELECT id,'TV',1    FROM tmp_room WHERE rn % 2 = 1;
INSERT INTO tmp_re SELECT id,'VC',1    FROM tmp_room WHERE rn = 3;
INSERT INTO tmp_re SELECT id,'PHONE',1 FROM tmp_room WHERE capacity >= 13;
INSERT INTO tmp_re SELECT id,'FLIP',2  FROM tmp_room WHERE capacity >= 13;
INSERT INTO "RoomEquipment" ("Id","RoomId","EquipmentId","Quantity","CreatedAt","UpdatedAt")
SELECT gen_random_uuid(), re.room_id, e.id, re.qty, (now() AT TIME ZONE 'utc'), NULL
FROM tmp_re re JOIN tmp_equip e ON e.code = re.ecode;

-- 8) Bloklamalar (16: 4 keçmiş, 6 cari, 6 gələcək)
CREATE TEMP TABLE tmp_rn (seq int, id uuid) ON COMMIT DROP;
INSERT INTO tmp_rn (seq, id)
SELECT row_number() OVER (ORDER BY bcode, lvl, rn), id FROM tmp_room;

CREATE TEMP TABLE tmp_black (seq int, start_ts timestamp, end_ts timestamp, reason text) ON COMMIT DROP;
INSERT INTO tmp_black (seq, start_ts, end_ts, reason) VALUES
 (1,  (now() AT TIME ZONE 'utc') - interval '15 day', (now() AT TIME ZONE 'utc') - interval '14 day', 'Köçürülmə işləri (tamamlanıb)'),
 (19, (now() AT TIME ZONE 'utc') - interval '10 day', (now() AT TIME ZONE 'utc') - interval '7 day',  'Əsaslı təmir (keçmiş)'),
 (34, (now() AT TIME ZONE 'utc') - interval '3 day',  (now() AT TIME ZONE 'utc') - interval '3 day' + interval '2 hour', 'Qısa təmir (keçmiş)'),
 (42, (now() AT TIME ZONE 'utc') - interval '1 day',  (now() AT TIME ZONE 'utc') - interval '1 day' + interval '4 hour', 'Təmizlik (keçmiş)'),
 (5,  (now() AT TIME ZONE 'utc') - interval '1 hour',  (now() AT TIME ZONE 'utc') + interval '1 hour',  'Təmizlik işləri (davam edir)'),
 (13, (now() AT TIME ZONE 'utc') - interval '30 minute', (now() AT TIME ZONE 'utc') + interval '90 minute', 'Qısa texniki yoxlama'),
 (22, (now() AT TIME ZONE 'utc') - interval '3 hour',  (now() AT TIME ZONE 'utc') + interval '3 hour',  'İclas gedir — rezerv'),
 (29, (now() AT TIME ZONE 'utc') - interval '1 day',   (now() AT TIME ZONE 'utc') + interval '1 day',   'Texniki xidmət (davam edir)'),
 (37, (now() AT TIME ZONE 'utc') - interval '2 day',   (now() AT TIME ZONE 'utc') + interval '3 day',   'Əsaslı təmir (davam edir)'),
 (44, (now() AT TIME ZONE 'utc') - interval '4 hour',  (now() AT TIME ZONE 'utc') + interval '20 hour', 'Tədbir hazırlığı'),
 (8,  (now() AT TIME ZONE 'utc') + interval '1 day',   (now() AT TIME ZONE 'utc') + interval '1 day' + interval '1 hour',  'Avadanlıq quraşdırılması'),
 (16, (now() AT TIME ZONE 'utc') + interval '2 day',   (now() AT TIME ZONE 'utc') + interval '2 day' + interval '2 hour',  'Prezentasiya hazırlığı'),
 (25, (now() AT TIME ZONE 'utc') + interval '3 day',   (now() AT TIME ZONE 'utc') + interval '3 day' + interval '4 hour',  'Müsahibə otağı rezervi'),
 (31, (now() AT TIME ZONE 'utc') + interval '5 day',   (now() AT TIME ZONE 'utc') + interval '8 day',   'Konfrans üçün rezerv'),
 (40, (now() AT TIME ZONE 'utc') + interval '7 day',   (now() AT TIME ZONE 'utc') + interval '7 day' + interval '3 hour',  'Təlim sessiyası'),
 (46, (now() AT TIME ZONE 'utc') + interval '10 day',  (now() AT TIME ZONE 'utc') + interval '11 day',  'Audit üçün bağlı');

INSERT INTO "RoomBlackouts" ("Id","RoomId","StartDate","EndDate","Reason","CreatedAt","UpdatedAt")
SELECT gen_random_uuid(), rn.id, bl.start_ts, bl.end_ts, bl.reason, (now() AT TIME ZONE 'utc'), NULL
FROM tmp_black bl JOIN tmp_rn rn ON rn.seq = bl.seq;

COMMIT;

-- Nəticə
SELECT 'Buildings' AS tbl, count(*) AS cnt FROM "Buildings"
UNION ALL SELECT 'Floors',        count(*) FROM "Floors"
UNION ALL SELECT 'Rooms',         count(*) FROM "Rooms"
UNION ALL SELECT 'Equipment',     count(*) FROM "Equipment"
UNION ALL SELECT 'RoomEquipment', count(*) FROM "RoomEquipment"
UNION ALL SELECT 'RoomBlackouts', count(*) FROM "RoomBlackouts";
