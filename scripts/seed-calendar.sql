/* ============================================================
   MRBSystem - Tam CALENDAR seed (PostgreSQL)
   Hər bina × hər həftə 7 rezervasiya (günə 1-2, fərqli saatlar)
   + hər bina × hər həftə 1 ədəd 1-saatlıq bloklama.
   Həftələr: keçən, bu, +3 (≈1 ay) = 5 həftə.
   Rezervasiyalar mövcud istifadəçilər arasında paylanır.
   Saatlar Bakı yerli vaxtı; bazaya UTC (−4 saat) saxlanılır.
   İDEMPOTENT: əvvəlcə BÜTÜN rezervasiya + bloklamalar silinir.

   İşə salma (Docker):
     docker exec -i mrb-postgres psql -U postgres -d MRBSystemDb \
       < scripts/seed-calendar.sql
   ============================================================ */

BEGIN;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "Rooms") THEN RAISE EXCEPTION 'Otaq yoxdur. Evvelce scripts/seed-demo-data.sql run edin.'; END IF;
  IF NOT EXISTS (SELECT 1 FROM "AspNetUsers") THEN RAISE EXCEPTION 'Istifadeci yoxdur.'; END IF;
END $$;

-- İstifadəçilər (növbəli təyinat üçün)
CREATE TEMP TABLE tmp_u ON COMMIT DROP AS
SELECT row_number() OVER (ORDER BY "NormalizedEmail") AS seq, "Id" AS uid FROM "AspNetUsers";

-- Binalar
CREATE TEMP TABLE tmp_b ON COMMIT DROP AS
SELECT row_number() OVER (ORDER BY "Name") AS bidx, "Id" AS bid FROM "Buildings";

-- Bina üzrə otaqlar (təsadüfi sıra) + say
CREATE TEMP TABLE tmp_rb ON COMMIT DROP AS
SELECT f."BuildingId" AS bid,
       row_number() OVER (PARTITION BY f."BuildingId" ORDER BY random()) AS rseq,
       r."Id" AS rid
FROM "Rooms" r JOIN "Floors" f ON f."Id" = r."FloorId";

CREATE TEMP TABLE tmp_rbc ON COMMIT DROP AS
SELECT bid, count(*)::int AS cnt FROM tmp_rb GROUP BY bid;

-- Həftələr
CREATE TEMP TABLE tmp_week (wkidx int, wkoff int) ON COMMIT DROP;
INSERT INTO tmp_week VALUES (1,-1),(2,0),(3,1),(4,2),(5,3);

-- Həftəlik 7 slot
CREATE TEMP TABLE tmp_slot (slotno int, dow int, hh int, mi int, dur int, title text) ON COMMIT DROP;
INSERT INTO tmp_slot VALUES
 (1,0, 9, 0, 60,'Komanda görüşü'),
 (2,0,14, 0, 60,'Satış zəngi'),
 (3,1,11, 0, 90,'Sprint planlaşdırma'),
 (4,2,10, 0, 60,'Həftəlik sinxron'),
 (5,2,15,30, 60,'Dizayn müzakirəsi'),
 (6,3,13, 0,120,'Məhsul demo'),
 (7,4,16, 0, 60,'Retrospektiv');

-- Köhnə calendar məlumatı silinir
DELETE FROM "Reservations";
DELETE FROM "RoomBlackouts";

-- Rezervasiyalar (bina × həftə × 7 slot = 210)
WITH gen AS (
    SELECT b.bidx, b.bid, w.wkoff,
           s.dow, s.hh, s.mi, s.dur, s.title, s.slotno,
           (w.wkoff * 7 + s.dow) AS dayoff,
           row_number() OVER (PARTITION BY b.bid ORDER BY w.wkoff, s.slotno) AS bord,
           row_number() OVER (ORDER BY b.bidx, w.wkoff, s.slotno)            AS gord
    FROM tmp_b b CROSS JOIN tmp_week w CROSS JOIN tmp_slot s
),
uc AS (SELECT count(*)::int AS cnt FROM tmp_u)
INSERT INTO "Reservations" ("Id","Title","StartDate","EndDate","Status","ReservedByUserId","RoomId","CreatedAt","UpdatedAt")
SELECT gen_random_uuid(), g.title,
       x.start_utc,
       x.start_utc + make_interval(mins => g.dur),
       'Confirmed',
       u.uid, rb.rid,
       (now() AT TIME ZONE 'utc'), NULL
FROM gen g
JOIN tmp_rbc c ON c.bid = g.bid
JOIN tmp_rb  rb ON rb.bid = g.bid AND rb.rseq = ((g.bord - 1) % c.cnt) + 1
CROSS JOIN uc
JOIN tmp_u u ON u.seq = ((g.gord - 1) % uc.cnt) + 1
CROSS JOIN LATERAL (
    SELECT (date_trunc('day', (now() AT TIME ZONE 'utc') + interval '4 hour')
            + make_interval(days => g.dayoff, hours => (g.hh + ((g.bidx - 1) % 3))::int, mins => g.mi)
            - interval '4 hour') AS start_utc
) x;

-- Bloklamalar (bina × həftə = 30, Çərşənbə axşamı 16:00, 1 saat)
WITH bgen AS (
    SELECT b.bid, w.wkoff,
           row_number() OVER (PARTITION BY b.bid ORDER BY w.wkoff) AS word
    FROM tmp_b b CROSS JOIN tmp_week w
)
INSERT INTO "RoomBlackouts" ("Id","RoomId","StartDate","EndDate","Reason","CreatedAt","UpdatedAt")
SELECT gen_random_uuid(), rb.rid,
       x.start_utc, x.start_utc + interval '1 hour',
       'Həftəlik texniki baxış',
       (now() AT TIME ZONE 'utc'), NULL
FROM bgen g
JOIN tmp_rbc c ON c.bid = g.bid
JOIN tmp_rb  rb ON rb.bid = g.bid AND rb.rseq = ((g.word - 1) % c.cnt) + 1
CROSS JOIN LATERAL (
    SELECT (date_trunc('day', (now() AT TIME ZONE 'utc') + interval '4 hour')
            + make_interval(days => g.wkoff * 7 + 1, hours => 16)
            - interval '4 hour') AS start_utc
) x;

COMMIT;

-- Nəticə
SELECT 'Reservations' AS tbl, count(*) AS cnt FROM "Reservations"
UNION ALL SELECT 'RoomBlackouts', count(*) FROM "RoomBlackouts";

SELECT b."Name" AS bina, count(*) AS rezerv
FROM "Reservations" res
JOIN "Rooms" rm ON rm."Id" = res."RoomId"
JOIN "Floors" f ON f."Id" = rm."FloorId"
JOIN "Buildings" b ON b."Id" = f."BuildingId"
GROUP BY b."Name" ORDER BY b."Name";
