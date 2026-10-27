/* ============================================================
   MRBSystem - Otaqları peşəkar adlarla yenidən adlandır (PostgreSQL)
   Mövcud otaqları YERİNDƏ adlandırır (silmir) — ona görə
   rezervasiyalar, bloklamalar, avadanlıqlar qorunur.
   Hər otaq qlobal olaraq fərqli peşəkar ad alır.

   İşə salma:
     docker exec -i mrb-postgres psql -U postgres -d MRBSystemDb < scripts/rename-rooms.sql
     (Neon üçün): docker exec -i mrb-postgres psql "<neon-uri>" < scripts/rename-rooms.sql
   ============================================================ */

WITH pool(ord, nm) AS (
    VALUES
    (1,'Executive'),(2,'Horizon'),(3,'Summit'),(4,'Atlas'),(5,'Nexus'),(6,'Pinnacle'),
    (7,'Vertex'),(8,'Zenith'),(9,'Catalyst'),(10,'Momentum'),(11,'Synergy'),(12,'Vanguard'),
    (13,'Pioneer'),(14,'Beacon'),(15,'Orbit'),(16,'Meridian'),(17,'Apex'),(18,'Quantum'),
    (19,'Fusion'),(20,'Cascade'),(21,'Spectrum'),(22,'Infinity'),(23,'Odyssey'),(24,'Voyager'),
    (25,'Discovery'),(26,'Frontier'),(27,'Keystone'),(28,'Cornerstone'),(29,'Landmark'),(30,'Milestone'),
    (31,'Endeavour'),(32,'Aspire'),(33,'Elevate'),(34,'Ascend'),(35,'Stratus'),(36,'Nimbus'),
    (37,'Aurora'),(38,'Eclipse'),(39,'Polaris'),(40,'Vega'),(41,'Orion'),(42,'Nova'),
    (43,'Titan'),(44,'Cosmos'),(45,'Pulse'),(46,'Ignite'),(47,'Forge'),(48,'Unity')
),
ordered AS (
    SELECT r."Id",
           row_number() OVER (ORDER BY b."Name", f."LevelNumber", r."Name") AS seq
    FROM "Rooms" r
    JOIN "Floors" f ON f."Id" = r."FloorId"
    JOIN "Buildings" b ON b."Id" = f."BuildingId"
),
cnt AS (SELECT count(*)::int AS c FROM pool)
UPDATE "Rooms" r
SET "Name" = p.nm,
    "UpdatedAt" = (now() AT TIME ZONE 'utc')
FROM ordered o
CROSS JOIN cnt
JOIN pool p ON p.ord = ((o.seq - 1) % cnt.c) + 1
WHERE r."Id" = o."Id";

-- Nəticə: ilk 15 otaq
SELECT b."Name" AS bina, f."LevelNumber" AS mertebe, r."Name" AS otaq
FROM "Rooms" r
JOIN "Floors" f ON f."Id" = r."FloorId"
JOIN "Buildings" b ON b."Id" = f."BuildingId"
ORDER BY b."Name", f."LevelNumber", r."Name"
LIMIT 15;
