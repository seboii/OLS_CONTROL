-- Sahte Siber'e örnek veri. ETL akışını canlıya dokunmadan doğrulamak için.
-- Gerçek Siber2022 verisi DEĞİLDİR.
USE Siber2022;
GO

DELETE FROM sbr_ulke; DELETE FROM sbr_sehir; DELETE FROM sbr_ilce;
DELETE FROM sbr_vergidaire; DELETE FROM sdn_rezervasyondurum;
DELETE FROM sbr_odemesekli; DELETE FROM sbr_departman; DELETE FROM sbr_malcinsi;
DELETE FROM skn_kapcins; DELETE FROM skn_kalem; DELETE FROM sbr_doviztur;
DELETE FROM sbr_dovizkur; DELETE FROM sky_kullanici; DELETE FROM skn_yukdurum;
DELETE FROM sbr_teslimsekli; DELETE FROM skn_pozisyondurum;
DELETE FROM skn_sabittanim; DELETE FROM sfy_muhasebeentegrekodu;
DELETE FROM skn_arac;
GO

INSERT INTO sbr_ulke (ulkeid, ad, telefonkod, kisaad) VALUES
  ('11111111-1111-1111-1111-111111111111', N'Türkiye', '90', ' TR '),
  ('22222222-2222-2222-2222-222222222222', N'Almanya', '49', ' DE ');

INSERT INTO sbr_sehir (sehirid, ad, ulkeid) VALUES
  ('33333333-3333-3333-3333-333333333333', N'İstanbul', '11111111-1111-1111-1111-111111111111'),
  ('33333333-3333-3333-3333-333333333334', N'İzmir',    '11111111-1111-1111-1111-111111111111');

INSERT INTO sbr_ilce (ilceid, ad, sehirid) VALUES
  ('44444444-4444-4444-4444-444444444441', N'Kadıköy', '33333333-3333-3333-3333-333333333333'),
  ('44444444-4444-4444-4444-444444444442', N'Konak',   '33333333-3333-3333-3333-333333333334');

INSERT INTO sbr_vergidaire (vergidaireid, ad, ozelkod, sehir) VALUES
  ('55555555-0000-0000-0000-000000000001', N'Kadıköy VD', 34, N'İstanbul');

INSERT INTO sdn_rezervasyondurum (durumid, ad, sirano) VALUES
  ('66666666-0000-0000-0000-000000000001', N'Teklif', '1'),
  ('66666666-0000-0000-0000-000000000002', N'Onaylandı', '2');

INSERT INTO sbr_odemesekli (odemesekliid, ad, kodu) VALUES
  ('77777777-0000-0000-0000-000000000001', N'Peşin', 'PSN');

INSERT INTO sbr_departman (departmanid, ad) VALUES
  ('88888888-0000-0000-0000-000000000001', N'Operasyon');

INSERT INTO sbr_malcinsi (malcinsid, ad) VALUES
  ('99999999-0000-0000-0000-000000000001', N'Tekstil');

INSERT INTO skn_kapcins (kapcinsid, ad, edikod) VALUES
  ('aaaa0000-0000-0000-0000-000000000001', N'Palet', 'PLT');

INSERT INTO skn_kalem (kalemid, ad) VALUES
  ('bbbb0000-0000-0000-0000-000000000001', N'Navlun'),
  ('bbbb0000-0000-0000-0000-000000000002', N'Gümrükleme');

INSERT INTO sbr_doviztur (rowguid, ad, kod) VALUES
  ('cccc0000-0000-0000-0000-000000000001', N'Euro', 'EUR'),
  ('cccc0000-0000-0000-0000-000000000002', N'Dolar', 'USD');

INSERT INTO sbr_dovizkur (tarih, dovizkod, dovizalis, dovizsatis, efektifalis, efektifsatis) VALUES
  ('2026-08-01', 'EUR', 47.10, 47.35, 47.05, 47.45),
  ('2026-08-01', 'USD', 40.20, 40.40, 40.15, 40.50);

INSERT INTO sky_kullanici (kullaniciid, ad, kod, email, engelle) VALUES
  ('dddd0000-0000-0000-0000-000000000001', N'Ahmet Yılmaz', 'AY', 'ahmet@siber.test', 0),
  ('dddd0000-0000-0000-0000-000000000002', N'Pasif Kullanıcı', 'PK', NULL, 1);

INSERT INTO skn_yukdurum (yukdurumid, ad, sirano) VALUES
  (1, N'Hazırlanıyor', 1), (2, N'Yolda', 2), (3, N'Teslim Edildi', 3);

INSERT INTO sbr_teslimsekli (teslimsekliid, edikod, ad) VALUES
  ('eeee0000-0000-0000-0000-000000000001', 'EXW', N'Ex Works');

INSERT INTO skn_pozisyondurum (pozisyondurumid, ad, yukdurumid, rowguid, sirano) VALUES
  (1, N'Açık', 1, 'ffff0000-0000-0000-0000-000000000001', 1);

-- Sabit tanımlar: grupkod'a göre bölünmüş listeler
INSERT INTO skn_sabittanim (sabittanimid, grupkod, ad, kod, ozelkod, ekkod) VALUES
  ('1000-0000-0001', 'ISTURU',                 N'İhracat',  1, NULL, 'IHR'),
  ('1000-0000-0002', 'ISTURU',                 N'İthalat',  2, NULL, 'ITH'),
  ('1000-0000-0003', 'ROMORKCINS',             N'Tenteli',  1, NULL, NULL),
  ('1000-0000-0004', 'TALIMATGELISSEKLI',      N'E-posta',  1, NULL, NULL),
  ('1000-0000-0005', 'YUKLEMETIP',             N'Komple',   1, NULL, NULL),
  ('1000-0000-0006', 'YUKTUR',                 N'Parsiyel', 1, NULL, NULL),
  ('1000-0000-0007', 'REZERVASYONTASIMASEKLI', N'Karayolu', 1, NULL, NULL),
  ('1000-0000-0008', 'ARACTIP',                N'Tır',      1, 10,   NULL),
  ('1000-0000-0009', 'ARACDURUM',              N'Müsait',   1, 20,   NULL),
  ('1000-0000-0010', 'ARACSAHIP',              N'Özmal',    1, 30,   'OZM'),
  ('1000-0000-0011', 'SEFERTUR',               N'Normal',   1, NULL, NULL);

-- Cariler: muhasebe kodu cari tipini belirliyor
INSERT INTO sbr_firma (firmaid, ad, adres1, telefon1, email, vergino, vergidaire, sirketid, aktif) VALUES
  ('aaaaaaa1-0000-0000-0000-000000000001', N'Anadolu Nakliyat', N'İstanbul', '2161112233', 'info@anadolu.test', '1234567890', N'Kadıköy VD', 'BA4888B1-A2B0-4142-B273-92481D932EAD', 1),
  ('aaaaaaa1-0000-0000-0000-000000000002', N'Ege Lojistik',     N'İzmir',    '2322223344', 'info@ege.test',     '9876543210', N'Konak VD',   'BA4888B1-A2B0-4142-B273-92481D932EAD', 1),
  ('aaaaaaa1-0000-0000-0000-000000000003', N'Marmara Tedarik',  N'Bursa',    '2243334455', 'info@marmara.test', '5555555555', N'Nilüfer VD', 'BA4888B1-A2B0-4142-B273-92481D932EAD', 1);

-- Anadolu -> 120 (müşteri), Marmara -> 320 (tedarikçi), Ege -> kod yok (ikisi de)
INSERT INTO sfy_muhasebeentegrekodu (entegread, muhasebekod) VALUES
  (N'Anadolu Nakliyat', '120.01.001'),
  (N'Marmara Tedarik',  '320.01.001');

INSERT INTO skn_arac (aracid, plakano, aractip, romorkcins, aracsahip, aracdurum, baglifirmaid, km, yici, uluslararasi, en, boy, yukseklik, kapasite) VALUES
  ('bbbb1111-0000-0000-0000-000000000001', '34ABC123', 1, NULL, 1, 1, 'aaaaaaa1-0000-0000-0000-000000000001', 250000, 1, 1, 2.5, 13.6, 2.7, 24000);
GO

PRINT 'ornek Siber verisi yuklendi';
GO
