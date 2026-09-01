-- skn_sabittanim / sdn_rezervasyondurum: init-reference.sql bu tabloları GERÇEK
-- sunucuya erişim olmadan, tahmini isim/kodlarla doldurmuştu (ör. "Tır", "Boşta/
-- Seferde", "Normal/Ekspres" - Siber'de bu değerler YOK). 192.168.1.101 üzerindeki
-- gerçek sunucudan salt-okunur sorgularla doğrulanan değerlerle değiştiriliyor -
-- DbSeeder.cs'teki (OLS.Business/Seed/DbSeeder.cs) gerçek kod/isim eşlemesinin
-- birebir aynısı, böylece mock ile canlı uygulamanın seed verisi tutarlı kalıyor.
--
-- ÖNEMLİ: Türkçe metin literalleri MUTLAKA N'...' (Unicode) olarak yazılmalı.
-- İlk sürümde bazı satırlarda N eksikti; sqlcmd bağlantının varsayılan
-- (Unicode olmayan) code page'iyle çeviriyor, ş/ı/İ/ğ gibi Windows-1252'de
-- karşılığı olmayan harfler sessizce başka karaktere dönüşüyor (ör. "Sipariş"
-- -> "Sipari?"), bu da WHERE ad = '...' eşleşmesini sessizce hiç tutturamıyordu.
USE Siber2022;
GO

DELETE FROM skn_sabittanim WHERE grupkod IN (
    N'ARACTIP', N'ROMORKCINS', N'ARACSAHIP', N'ARACDURUM', N'SEFERTUR', N'YUKTUR',
    N'TALIMATGELISSEKLI', N'REZERVASYONTASIMASEKLI', N'ISTURU', N'YUKLEMETIP');
GO

INSERT INTO skn_sabittanim (sabittanimid, grupkod, ad, kod, ekkod) VALUES
    ('ref-aractip-0', N'ARACTIP', N'ÇEKİCİ', 0, NULL),
    ('ref-aractip-1', N'ARACTIP', N'KAMYON', 1, NULL),
    ('ref-aractip-2', N'ARACTIP', N'ROMORK', 2, NULL),
    ('ref-aractip-3', N'ARACTIP', N'OTOMOBIL', 3, NULL),
    ('ref-aractip-4', N'ARACTIP', N'KONTEYNER', 4, NULL),

    ('ref-romorkcins-0', N'ROMORKCINS', N'FRIGO', 0, NULL),
    ('ref-romorkcins-1', N'ROMORKCINS', N'JUMBO', 1, NULL),
    ('ref-romorkcins-2', N'ROMORKCINS', N'ROMORK [KAMYON]', 2, NULL),
    ('ref-romorkcins-3', N'ROMORKCINS', N'OPTIMA', 3, NULL),
    ('ref-romorkcins-4', N'ROMORKCINS', N'TANKER', 4, NULL),
    ('ref-romorkcins-5', N'ROMORKCINS', N'TEKSTIL DORSE', 5, NULL),
    ('ref-romorkcins-6', N'ROMORKCINS', N'OTO TAŞIYICI', 6, NULL),
    ('ref-romorkcins-7', N'ROMORKCINS', N'SILOBAS', 7, NULL),
    ('ref-romorkcins-8', N'ROMORKCINS', N'LOW BED', 8, NULL),
    ('ref-romorkcins-9', N'ROMORKCINS', N'MEGA MAKSİMA', 9, NULL),
    ('ref-romorkcins-10', N'ROMORKCINS', N'MAKSİMA', 10, NULL),
    ('ref-romorkcins-11', N'ROMORKCINS', N'MEGA', 11, NULL),

    -- ekkod = skn_sefer.aracsahipad (nvarchar(10)) kısa kodu; FindSeferIdAsync bununla eşleşiyor.
    ('ref-aracsahip-0', N'ARACSAHIP', N'ÖZMAL', 0, 'OZ'),
    ('ref-aracsahip-1', N'ARACSAHIP', N'KİRALIK', 1, 'KR'),
    ('ref-aracsahip-2', N'ARACSAHIP', N'SÖZLEŞMELİ KİRALIK', 2, 'SK'),
    ('ref-aracsahip-3', N'ARACSAHIP', N'KONTEYNER KİRALIK', 3, 'KK'),
    ('ref-aracsahip-4', N'ARACSAHIP', N'KONTEYNER ÖZMAL', 4, 'KO'),
    ('ref-aracsahip-5', N'ARACSAHIP', N'KONTEYNER SÖZLEŞMELİ', 5, 'KS'),

    ('ref-aracdurum-0', N'ARACDURUM', N'ÇALIŞAN', 0, NULL),
    ('ref-aracdurum-1', N'ARACDURUM', N'BAKIMDA', 1, NULL),
    ('ref-aracdurum-2', N'ARACDURUM', N'HURDA', 2, NULL),
    ('ref-aracdurum-3', N'ARACDURUM', N'SATILDI', 3, NULL),
    ('ref-aracdurum-4', N'ARACDURUM', N'KOMBİNASYONDA', 4, NULL),

    ('ref-sefertur-10', N'SEFERTUR', N'KARA', 10, NULL),
    ('ref-sefertur-11', N'SEFERTUR', N'HAVA', 11, NULL),
    ('ref-sefertur-12', N'SEFERTUR', N'DENİZ', 12, NULL),

    ('ref-yuktur-1', N'YUKTUR', N'KARA', 1, NULL),
    ('ref-yuktur-2', N'YUKTUR', N'HAVA', 2, NULL),
    ('ref-yuktur-3', N'YUKTUR', N'DENİZ', 3, NULL),

    ('ref-talimat-0', N'TALIMATGELISSEKLI', N'TELEFON', 0, NULL),
    ('ref-talimat-1', N'TALIMATGELISSEKLI', N'E-MAIL', 1, NULL),
    ('ref-talimat-2', N'TALIMATGELISSEKLI', N'FAKS', 2, NULL),
    ('ref-talimat-3', N'TALIMATGELISSEKLI', N'PAZARLAMA', 3, NULL),

    -- sabittanimid'ler DbSeeder.cs TransportTypes.SiberId ile birebir - gerçek sunucudan
    -- doğrulandı ve olsold'un kendi system_data.js sabit listesiyle çapraz eşleşti.
    ('9E45ED23-EF9F-45E4-9530-0FA9F2D6C51C', N'REZERVASYONTASIMASEKLI', N'RO-RO', 1, NULL),
    ('E0ADF7B0-6711-48ED-B2F5-FFBDEBD405A2', N'REZERVASYONTASIMASEKLI', N'TREN', 2, NULL),
    ('B84B6983-7328-469C-8CBE-58E4AB2B3DB4', N'REZERVASYONTASIMASEKLI', N'KARA', 3, NULL),

    ('ref-isturu-0', N'ISTURU', N'İHRACAT', 0, 'EX'),
    ('ref-isturu-1', N'ISTURU', N'İTHALAT', 1, 'IM'),
    ('ref-isturu-2', N'ISTURU', N'TRANSİT', 2, 'TR'),
    ('ref-isturu-3', N'ISTURU', N'YURTİÇİ', 3, 'YI'),

    ('ref-yuklemetip-0', N'YUKLEMETIP', N'PARSİYEL', 0, NULL),
    ('ref-yuklemetip-1', N'YUKLEMETIP', N'KOMPLE', 1, NULL);
GO

-- sdn_rezervasyondurum: init-reference.sql/sample-data.sql rastgele 66666666-...
-- GUID'lerle dolduruyordu. TransferSiberService.TransferOfferAsync durumid'yi
-- StatusType.SiberId'den okuyor (DbSeeder.cs SeedStatusTypesAsync); ikisi
-- eşleşmezse mock'a karşı test edilen bir teklif aktarımı canlıdakinden
-- FARKLI (ama yine de "çalışan") bir GUID'le geçer - gerçek sunucudan
-- doğrulanan GUID'lerle değiştirildi ki mock canlıyla tutarlı olsun.
UPDATE sdn_rezervasyondurum SET durumid = '5E0B49DD-E425-4537-90F2-710EEB44A19F' WHERE ad = N'Olumsuz';
UPDATE sdn_rezervasyondurum SET durumid = 'DDF0614E-CA55-4C26-B125-A3AEBFAFB20B' WHERE ad = N'Sipariş';
UPDATE sdn_rezervasyondurum SET durumid = 'FCF55F7C-876A-482B-B4A7-BADCA250BB91' WHERE ad = N'Düzeltme Talebi';
UPDATE sdn_rezervasyondurum SET durumid = 'EC922C9E-C2CF-4716-A198-F716FDA50358' WHERE ad = N'Teklif';
UPDATE sdn_rezervasyondurum SET durumid = 'F377242D-0121-4090-BDD2-FF420F21235A' WHERE ad = N'Olumlu';
GO

PRINT 'referans verileri gercek Siber degerleriyle guncellendi';
GO
