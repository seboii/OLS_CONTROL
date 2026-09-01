-- Sahte Siber: geçmiş hareket verisi (yük / sefer / yük aktarma).
-- pullLoad, pull_expdition ve pull_skn_yukaktarma akışlarını doğrulamak için.
USE Siber2022;
GO

-- init.sql'in ürettiği iskeletler ETL'in okuduğu kolonları içermiyor;
-- hareket tablolarını da doğru kolonlarla yeniden kuruyoruz.
DROP TABLE IF EXISTS skn_yuk, skn_yukkoli, sfy_modulkalem, skn_pozisyon, skn_yukaktarma;
GO

CREATE TABLE skn_yuk (
    yukid NVARCHAR(64), yukno NVARCHAR(64), isturu NVARCHAR(64),
    bagliyukno NVARCHAR(64), durumid NVARCHAR(64), yuklemetip NVARCHAR(64),
    firmaid NVARCHAR(64), gondericiid NVARCHAR(64), aliciid NVARCHAR(64),
    odemesekliid NVARCHAR(64), talimatgelissekli NVARCHAR(64), istenenromorkcins NVARCHAR(64),
    musteritemsilcisiad NVARCHAR(255), musteritemsilcisi2ad NVARCHAR(255),
    departmanid NVARCHAR(64), yukturkod NVARCHAR(64),
    bildirimyapankullanicikod NVARCHAR(64), satistemsilcisikod NVARCHAR(64),
    teslimsekil NVARCHAR(64),
    kamyonda INT, kuyrukta INT, cmrduzenlenecek INT, fcrduzenlenecek INT,
    toplamagirlik DECIMAL(18,4), toplamhacim DECIMAL(18,4), toplamlademetre DECIMAL(18,4),
    ucretagirlik DECIMAL(18,4), yuknoisturu NVARCHAR(64), bagliyuknoisturu NVARCHAR(64),
    toplamkap DECIMAL(18,4), toplamlademetrem3 DECIMAL(18,4),
    _yuklemeulke NVARCHAR(64), _bosaltmaulke NVARCHAR(64),
    _yuklemekita NVARCHAR(64), _bosaltmakita NVARCHAR(64),
    calismasekli INT, ontasimatarafimizdanyapilir INT, sontasimatarafimizdanyapilir INT,
    talimatgelistarihi DATETIME, istenenvaristarihi DATETIME,
    hazirolmatarih DATETIME, musteridenalinistarih DATETIME);

CREATE TABLE skn_yukkoli (
    yukkoliid NVARCHAR(64), yukid NVARCHAR(64), kapadet INT, kapid NVARCHAR(64),
    en DECIMAL(18,4), boy DECIMAL(18,4), yukseklik DECIMAL(18,4), hacim DECIMAL(18,4),
    burutagirlik DECIMAL(18,4), netagirlik DECIMAL(18,4), lademetre DECIMAL(18,4),
    istiflenemez INT, malcinsid NVARCHAR(64));

CREATE TABLE sfy_modulkalem (
    modulkalemid NVARCHAR(64), modulid NVARCHAR(64), modulkod NVARCHAR(64),
    kalemid NVARCHAR(64), gc NVARCHAR(4), firmaid NVARCHAR(64),
    toplamtutar DECIMAL(18,4), dovizkod NVARCHAR(16), birimfiyat DECIMAL(18,4),
    miktar DECIMAL(18,4), aciklama NVARCHAR(500), kayitad NVARCHAR(64), kayitgiren NVARCHAR(64));

CREATE TABLE skn_pozisyon (
    pozisyonid NVARCHAR(64), seferno NVARCHAR(64), seferid NVARCHAR(64),
    isturu NVARCHAR(64), durumid NVARCHAR(64), romorkid NVARCHAR(64),
    hafta NVARCHAR(32), departmanid NVARCHAR(64), kayitgiristarih DATETIME,
    seferturid NVARCHAR(64), araccikistarih DATETIME, cikistarih DATETIME,
    yuklemetarih DATETIME, donustarih DATETIME,
    baslangicsehirid NVARCHAR(64), yuklemesehirid NVARCHAR(64), bitissehirid NVARCHAR(64));

CREATE TABLE skn_yukaktarma (
    yukaktarmaid NVARCHAR(64), yuklemebosaltma INT, yukid NVARCHAR(64),
    pozisyonid NVARCHAR(64), romorkid NVARCHAR(64), yerid NVARCHAR(64), tarih DATETIME);
GO

-- Örnek yükler ------------------------------------------------------------
INSERT INTO skn_yuk (yukid, yukno, yuknoisturu, isturu, durumid, yuklemetip, firmaid, gondericiid, aliciid,
    odemesekliid, departmanid, yukturkod, teslimsekil, toplamagirlik, toplamhacim, toplamlademetre, toplamkap,
    _yuklemeulke, _bosaltmaulke, kamyonda, kuyrukta, cmrduzenlenecek, fcrduzenlenecek,
    talimatgelistarihi, istenenvaristarihi, hazirolmatarih, musteridenalinistarih) VALUES
  ('yuk-0000-0001', 'YK2026001', 'YK2026001-IHR', 1, 2, 1,
   'aaaaaaa1-0000-0000-0000-000000000001', 'aaaaaaa1-0000-0000-0000-000000000001', 'aaaaaaa1-0000-0000-0000-000000000002',
   '77777777-0000-0000-0000-000000000001', '88888888-0000-0000-0000-000000000001', 1, 'EXW',
   18500.00, 62.50, 8.20, 24, '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222',
   0, 0, 1, 0, '2026-07-10', '2026-07-20', '2026-07-12', '2026-07-09'),
  ('yuk-0000-0002', 'YK2026002', 'YK2026002-ITH', 2, 1, 1,
   'aaaaaaa1-0000-0000-0000-000000000003', 'aaaaaaa1-0000-0000-0000-000000000003', 'aaaaaaa1-0000-0000-0000-000000000001',
   '77777777-0000-0000-0000-000000000001', '88888888-0000-0000-0000-000000000001', 1, 'EXW',
   9200.00, 31.00, 4.10, 12, '22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111',
   1, 0, 0, 1, '2026-07-15', '2026-07-25', '2026-07-18', '2026-07-14');

INSERT INTO skn_yukkoli (yukkoliid, yukid, kapadet, kapid, en, boy, yukseklik, hacim, burutagirlik, netagirlik, lademetre, istiflenemez, malcinsid) VALUES
  ('koli-0000-0001', 'yuk-0000-0001', 24, 'aaaa0000-0000-0000-0000-000000000001', 1.2, 0.8, 1.8, 62.5, 18500, 17800, 8.2, 0, '99999999-0000-0000-0000-000000000001'),
  ('koli-0000-0002', 'yuk-0000-0002', 12, 'aaaa0000-0000-0000-0000-000000000001', 1.2, 0.8, 1.5, 31.0, 9200, 8900, 4.1, 1, '99999999-0000-0000-0000-000000000001');

INSERT INTO sfy_modulkalem (modulkalemid, modulid, modulkod, kalemid, gc, firmaid, toplamtutar, dovizkod, birimfiyat, miktar, aciklama, kayitad, kayitgiren) VALUES
  ('kalem-0000-0001', 'mod-1', 'YUK', 'bbbb0000-0000-0000-0000-000000000001', 'C', 'aaaaaaa1-0000-0000-0000-000000000001', 2500.00, 'EUR', 2500.00, 1, N'Navlun bedeli', 'YK2026001-IHR', 'AY'),
  ('kalem-0000-0002', 'mod-1', 'YUK', 'bbbb0000-0000-0000-0000-000000000002', 'G', 'aaaaaaa1-0000-0000-0000-000000000002', 450.00,  'EUR', 450.00,  1, N'Gümrükleme',    'YK2026001-IHR', 'AY');

-- Seferler ----------------------------------------------------------------
INSERT INTO skn_pozisyon (pozisyonid, seferno, seferid, isturu, durumid, romorkid, hafta, departmanid,
    kayitgiristarih, seferturid, araccikistarih, cikistarih, yuklemetarih, donustarih,
    baslangicsehirid, yuklemesehirid, bitissehirid) VALUES
  ('poz-0000-0001', 'SF2026001', 'sefer-1', 1, 1, 'bbbb1111-0000-0000-0000-000000000001', '2026-28',
   '88888888-0000-0000-0000-000000000001', '2026-07-10', 1, '2026-07-12', '2026-07-12', '2026-07-11', '2026-07-22',
   '33333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333334');

-- Yük aktarma -------------------------------------------------------------
INSERT INTO skn_yukaktarma (yukaktarmaid, yuklemebosaltma, yukid, pozisyonid, romorkid, yerid, tarih) VALUES
  ('akt-0000-0001', 1, 'yuk-0000-0001', 'poz-0000-0001', 'bbbb1111-0000-0000-0000-000000000001', '33333333-3333-3333-3333-333333333333', '2026-07-11');
GO

PRINT 'ornek hareket verisi yuklendi';
GO
