/* ============================================================
   MRBSystem - Demo REZERVASIYALAR (PostgreSQL)
   İstifadəçi: seli.zeynalzade.2006@gmail.com
   Çoxlu gələcək (bu/gələn həftə sıx, 1 aya qədər) + bir qədər keçmiş.
   Saatlar Bakı yerli vaxtı; bazaya UTC (−4 saat) saxlanılır.
   Təkrar run: bu istifadəçinin köhnə rezervasiyalarını silir.

   İşə salma (Docker):
     docker exec -i mrb-postgres psql -U postgres -d MRBSystemDb \
       < scripts/seed-reservations.sql
   ============================================================ */

BEGIN;

-- İstifadəçi və otaqlar yoxlanılır
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "NormalizedEmail" = upper('seli.zeynalzade.2006@gmail.com')) THEN
    RAISE EXCEPTION 'Istifadeci tapilmadi. Evvelce bu email ile qeydiyyatdan kecin: seli.zeynalzade.2006@gmail.com';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM "Rooms") THEN
    RAISE EXCEPTION 'Otaq yoxdur. Evvelce scripts/seed-demo-data.sql run edin.';
  END IF;
END $$;

-- Hədəf istifadəçi
CREATE TEMP TABLE tmp_user ON COMMIT DROP AS
SELECT "Id" AS uid FROM "AspNetUsers"
WHERE "NormalizedEmail" = upper('seli.zeynalzade.2006@gmail.com') LIMIT 1;

-- Köhnə rezervasiyalar silinir (Participant-lar cascade)
DELETE FROM "Reservations" WHERE "ReservedByUserId" IN (SELECT uid FROM tmp_user);

-- Otaqlar təsadüfi sıra ilə
CREATE TEMP TABLE tmp_rooms ON COMMIT DROP AS
SELECT row_number() OVER (ORDER BY random()) AS seq, "Id" AS rid FROM "Rooms";

-- Rezervasiya siyahısı (dayoff: bu gündən fərq; saat/dəqiqə Bakı vaxtı)
CREATE TEMP TABLE tmp_res (ridx int, dayoff int, hh int, mi int, dur int, title text, status text) ON COMMIT DROP;
INSERT INTO tmp_res (ridx, dayoff, hh, mi, dur, title, status) VALUES
 (1, -14,10, 0, 60,'Komanda görüşü','Confirmed'),
 (2, -10,14,30, 90,'Layihə təqdimatı','Confirmed'),
 (3,  -7, 9, 0,120,'Təlim sessiyası','Confirmed'),
 (4,  -5,16, 0, 60,'Müştəri görüşü','Cancelled'),
 (5,  -3,11, 0, 45,'1:1 görüş','Confirmed'),
 (6,  -1,15, 0, 60,'Retrospektiv','Confirmed'),
 (7,   0,13, 0, 60,'Gündəlik standup','Confirmed'),
 (8,   0,17, 0, 60,'QA sinxron','Confirmed'),
 (9,   1,10, 0, 90,'Sprint planlaşdırma','Confirmed'),
 (10,  1,15,30, 60,'Dizayn müzakirəsi','Confirmed'),
 (11,  2, 9,30, 60,'Həftəlik sinxron','Confirmed'),
 (12,  2,14, 0,120,'Məhsul demo','Confirmed'),
 (13,  3,11, 0, 60,'Marketinq brifinqi','Confirmed'),
 (14,  4,16, 0, 90,'Strategiya görüşü','Confirmed'),
 (15,  5,10, 0, 60,'Satış zəngi','Confirmed'),
 (16,  6,13,30, 60,'Texniki müzakirə','Confirmed'),
 (17,  7,10, 0, 60,'Komanda görüşü','Confirmed'),
 (18,  8,14, 0, 90,'Büdcə icmalı','Confirmed'),
 (19,  9, 9, 0,120,'Onboarding','Confirmed'),
 (20,  9,15, 0, 60,'Müsahibə','Confirmed'),
 (21, 10,11,30, 60,'Roadmap icmalı','Confirmed'),
 (22, 11,16,30, 60,'Partnyor görüşü','Confirmed'),
 (23, 12,10, 0, 90,'İdarə heyəti iclası','Confirmed'),
 (24, 13,13, 0, 60,'Retrospektiv','Confirmed'),
 (25, 17,10, 0, 60,'Layihə təqdimatı','Confirmed'),
 (26, 20,14, 0, 90,'Strategiya görüşü','Confirmed'),
 (27, 24,11, 0, 60,'Müştəri görüşü','Confirmed'),
 (28, 28,15, 0, 60,'Təlim sessiyası','Confirmed'),
 (29, 30,10, 0,120,'Planlaşdırma seminarı','Confirmed');

INSERT INTO "Reservations" ("Id","Title","StartDate","EndDate","Status","ReservedByUserId","RoomId","CreatedAt","UpdatedAt")
SELECT gen_random_uuid(), r.title,
       s.start_utc,
       s.start_utc + make_interval(mins => r.dur),
       r.status,
       (SELECT uid FROM tmp_user),
       rm.rid,
       (now() AT TIME ZONE 'utc'), NULL
FROM tmp_res r
CROSS JOIN (SELECT count(*)::int AS rc FROM tmp_rooms) b
JOIN tmp_rooms rm ON rm.seq = ((r.ridx - 1) % b.rc) + 1
CROSS JOIN LATERAL (
    SELECT (date_trunc('day', (now() AT TIME ZONE 'utc') + interval '4 hour')
            + make_interval(days => r.dayoff, hours => r.hh, mins => r.mi)
            - interval '4 hour') AS start_utc
) s;

COMMIT;

-- Nəticə
SELECT CASE WHEN "EndDate" < (now() AT TIME ZONE 'utc') THEN 'KECMIS'
            WHEN (now() AT TIME ZONE 'utc') BETWEEN "StartDate" AND "EndDate" THEN 'CARI'
            ELSE 'GELECEK' END AS status,
       count(*) AS cnt
FROM "Reservations"
WHERE "ReservedByUserId" = (SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail" = upper('seli.zeynalzade.2006@gmail.com'))
GROUP BY 1;
