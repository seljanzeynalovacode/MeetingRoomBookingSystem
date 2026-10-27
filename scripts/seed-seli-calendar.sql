/* ============================================================
   MRBSystem - seli üçün tam 2-aylıq CALENDAR (PostgreSQL)
   - Bütün rezervasiyalar seli.zeynalzade.2006@gmail.com hesabına
     (admin və digər user-lərdə heç bir rezervasiya qalmır).
   - Hər otaqda hər iş günü 1-2 rezervasiya (09:00-12:00 və 14:00-17:00),
     ≈2 ay (−1 həftə keçmiş .. +8 həftə gələcək), peşəkar başlıqlar.
   - Bloklamalar: hər otaqda hər həftə ≥2 ədəd, iş günündə, iş saatları
     daxilində (12:00-dan 1-2 saat), peşəkar səbəblərlə → calendarda görünür.
   İDEMPOTENT: əvvəlcə BÜTÜN rezervasiya + bloklamalar silinir.

   İşə salma:
     docker exec -i mrb-postgres psql -U postgres -d MRBSystemDb < scripts/seed-seli-calendar.sql
     (Neon): docker exec -i mrb-postgres psql "<neon-uri>" < scripts/seed-seli-calendar.sql
   ============================================================ */

BEGIN;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "NormalizedEmail"=upper('seli.zeynalzade.2006@gmail.com')) THEN
    RAISE EXCEPTION 'Istifadeci tapilmadi: seli.zeynalzade.2006@gmail.com';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM "Rooms") THEN
    RAISE EXCEPTION 'Otaq yoxdur. Evvelce seed-demo-data.sql run edin.';
  END IF;
END $$;

DELETE FROM "Reservations";
DELETE FROM "RoomBlackouts";

CREATE TEMP TABLE t_uid ON COMMIT DROP AS
SELECT "Id" AS uid FROM "AspNetUsers" WHERE "NormalizedEmail"=upper('seli.zeynalzade.2006@gmail.com') LIMIT 1;

CREATE TEMP TABLE t_now ON COMMIT DROP AS
SELECT ((now() AT TIME ZONE 'utc' + interval '4 hour')::date) AS today;

CREATE TEMP TABLE t_rooms ON COMMIT DROP AS
SELECT "Id" AS rid, (row_number() OVER (ORDER BY "Id"))::int AS rseq FROM "Rooms";

-- Yalnız iş günləri (Be-Cümə), ≈2 ay
CREATE TEMP TABLE t_days ON COMMIT DROP AS
SELECT d::date AS day,
       (d::date - (SELECT today FROM t_now))::int AS dayoff,
       extract(isodow FROM d)::int AS idow
FROM generate_series(
    (SELECT today FROM t_now) - interval '7 day',
    (SELECT today FROM t_now) + interval '56 day',
    interval '1 day') d
WHERE extract(isodow FROM d) BETWEEN 1 AND 5;

CREATE TEMP TABLE t_titles(ord int, t text) ON COMMIT DROP;
INSERT INTO t_titles VALUES
 (0,'Strategy Review'),(1,'Quarterly Planning'),(2,'Client Onboarding'),(3,'Product Roadmap'),
 (4,'Board Meeting'),(5,'Design Sprint'),(6,'Sales Pipeline Review'),(7,'Budget Review'),
 (8,'Stakeholder Sync'),(9,'Project Kickoff'),(10,'Performance Review'),(11,'Vendor Negotiation'),
 (12,'Market Analysis'),(13,'Technical Deep Dive'),(14,'Leadership Sync'),(15,'Customer Success Review'),
 (16,'Innovation Workshop'),(17,'Risk Assessment'),(18,'All-Hands Briefing'),(19,'Sprint Retrospective');

CREATE TEMP TABLE t_reasons(ord int, t text) ON COMMIT DROP;
INSERT INTO t_reasons VALUES
 (0,'Scheduled Maintenance'),(1,'Technical Inspection'),(2,'Deep Cleaning'),(3,'AV Equipment Upgrade'),
 (4,'HVAC Servicing'),(5,'Facility Audit'),(6,'Network Maintenance'),(7,'Safety Inspection');

/* ---------- Rezervasiyalar (seli) — 09:00-12:00 və 14:00-17:00 ---------- */
WITH slots AS (
    SELECT rm.rid, rm.rseq, dy.day, dy.dayoff, s.slotno
    FROM t_rooms rm
    CROSS JOIN t_days dy
    CROSS JOIN generate_series(1,2) s(slotno)
    WHERE s.slotno = 1 OR ((rm.rseq + dy.dayoff) % 3 = 0)   -- 2-ci rezerv ~hər 3 gündən 1-ində
),
computed AS (
    SELECT sl.rid, sl.day, sl.dayoff, sl.slotno,
           (ARRAY[9,11,14,10,15,16])[ (((sl.rseq + sl.dayoff + (sl.slotno-1)*3) % 6) + 6) % 6 + 1 ] AS hh,
           (ARRAY[45,60,50,60])[ (((sl.rseq + sl.dayoff) % 4) + 4) % 4 + 1 ] AS dur,
           (((sl.rseq*7 + sl.dayoff*3 + sl.slotno) % 20) + 20) % 20 AS titleord
    FROM slots sl
)
INSERT INTO "Reservations" ("Id","Title","StartDate","EndDate","Status","ReservedByUserId","RoomId","CreatedAt","UpdatedAt")
SELECT gen_random_uuid(), tt.t, stt.startutc,
       stt.startutc + make_interval(mins => c.dur),
       'Confirmed', (SELECT uid FROM t_uid), c.rid,
       (now() AT TIME ZONE 'utc'), NULL
FROM computed c
JOIN t_titles tt ON tt.ord = c.titleord
CROSS JOIN LATERAL (
    SELECT (c.day::timestamp + make_interval(hours => c.hh)) - interval '4 hour' AS startutc
) stt;

/* ---------- Bloklamalar — hər otaq, həftədə 2, iş günü, 12:00 (1-2 saat) ---------- */
INSERT INTO "RoomBlackouts" ("Id","RoomId","StartDate","EndDate","Reason","CreatedAt","UpdatedAt")
SELECT gen_random_uuid(), rm.rid,
       (dy.day::timestamp + make_interval(hours => 12)) - interval '4 hour',
       (dy.day::timestamp + make_interval(hours => 12 + (((rm.rseq + dy.dayoff) % 2) + 2) % 2 + 1)) - interval '4 hour',
       rr.t,
       (now() AT TIME ZONE 'utc'), NULL
FROM t_rooms rm
CROSS JOIN t_days dy
JOIN t_reasons rr ON rr.ord = (((rm.rseq + dy.dayoff) % 8) + 8) % 8
WHERE ((dy.idow + rm.rseq) % 5) IN (0, 1);     -- hər otaq üçün həftədə 2 iş günü

COMMIT;

/* ---------- Nəticə ---------- */
SELECT 'Reservations' AS tbl, count(*) AS cnt FROM "Reservations"
UNION ALL SELECT 'RoomBlackouts', count(*) FROM "RoomBlackouts";

-- Bir otaq üçün həftəlik bloklama sayı (≥2 olmalı)
SELECT count(*) AS bir_otaqda_bloklama
FROM "RoomBlackouts" b
WHERE b."RoomId" = (SELECT "Id" FROM "Rooms" ORDER BY "Id" LIMIT 1);
