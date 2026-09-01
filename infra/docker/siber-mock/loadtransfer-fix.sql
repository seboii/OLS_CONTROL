-- Teklif -> yük dönüşümünün ihtiyaç duyduğu Siber tabloları.
USE Siber2022;
GO

-- skn_yuk: yazma tarafının kullandığı ek kolonlar
ALTER TABLE skn_yuk ADD
    sirketid NVARCHAR(64) NULL, subeid NVARCHAR(64) NULL,
    operasyondepartmanid NVARCHAR(64) NULL, kayitgiristarih DATETIME NULL,
    kayitgiren NVARCHAR(64) NULL, yil NVARCHAR(8) NULL,
    lademetrecarpan DECIMAL(18,4) NULL, hacimcarpan DECIMAL(18,4) NULL,
    aracyuksekligi INT NULL;
GO

-- skn_rezervasyon: dönüşüm öncesi karşılaştırma yapılan tablo (ConvertOfferAsync
-- ->FindRezervasyonAsync okuyor). BURADA YENİDEN OLUŞTURULMUYOR: reservation-fix.sql
-- zaten geniş şemayı (transfer_to_siber'in INSERT'inin ihtiyaç duyduğu 31 kolon,
-- bkz. SiberReservationRepository.InsertRezervasyonAsync) kuruyor ve ConvertOfferAsync'in
-- okuduğu 9 kolonun hepsi onun içinde. Bu dosya DAHA ÖNCE dar bir şemayla burayı
-- DROP+CREATE ediyordu — reservation-fix.sql'den SONRA çalışırsa transfer_to_siber'in
-- INSERT'i eksik kolon yüzünden patlıyordu. Bulunup düzeltildi.

-- sfy_modulkayit: fatura kalemlerinin modül kodu. "yer" gerçek Siber'de kalemin
-- HANGİ nesneye bağlı olduğunu söylüyor (YUK/SEFER/FIRMA/ARAC/PERSONEL/rzv) —
-- canlı Siber'de doğrulandı, senkron artık bununla yalnızca YUK'u filtreliyor.
DROP TABLE IF EXISTS sfy_modulkayit;
CREATE TABLE sfy_modulkayit (
    ad NVARCHAR(64), modulid NVARCHAR(64), modulkod NVARCHAR(64), yer NVARCHAR(64));
GO

-- sfy_modulkalem: yazma için ek kolonlar
ALTER TABLE sfy_modulkalem ADD
    kdvtutar DECIMAL(18,4) NULL, kdvoran DECIMAL(18,4) NULL,
    tutar DECIMAL(18,4) NULL, subeid NVARCHAR(64) NULL,
    kayitgiristarih DATETIME NULL, rezervasyondanaktarildi INT NULL;
GO

-- skn_yukevrak: Evrak Takibi (fiziksel evrak çeklisti — gerçek dosya değil).
-- Gerçek Siber'de doğrulandı: evrakid hiçbir tabloya FK vermiyor (rastgele
-- üretiliyor), sirano (1-10) sabit evrak türü kodu.
DROP TABLE IF EXISTS skn_yukevrak;
CREATE TABLE skn_yukevrak (
    evrakid NVARCHAR(64) PRIMARY KEY, yukid NVARCHAR(64), sirano INT,
    evrakad NVARCHAR(128), evrakno NVARCHAR(64), tarih DATETIME,
    orjinaladet INT, kopyaadet INT, teslimalan NVARCHAR(128),
    teslimtarih DATETIME, aciklama NVARCHAR(512));
GO

PRINT 'loadtransfer tablolari hazir';
GO
