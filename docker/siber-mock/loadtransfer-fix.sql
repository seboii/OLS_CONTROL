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

-- skn_rezervasyon: dönüşüm öncesi karşılaştırma yapılan tablo
DROP TABLE IF EXISTS skn_rezervasyon;
CREATE TABLE skn_rezervasyon (
    rezervasyonid NVARCHAR(64), istenenromorkcins NVARCHAR(64), isturu NVARCHAR(64),
    musteriid NVARCHAR(64), gondericiid NVARCHAR(64), aliciid NVARCHAR(64),
    odemesekliid NVARCHAR(64), durumid NVARCHAR(64), departmanid NVARCHAR(64));
GO

-- sfy_modulkayit: fatura kalemlerinin modül kodu
DROP TABLE IF EXISTS sfy_modulkayit;
CREATE TABLE sfy_modulkayit (
    ad NVARCHAR(64), modulid NVARCHAR(64), modulkod NVARCHAR(64));
GO

-- sfy_modulkalem: yazma için ek kolonlar
ALTER TABLE sfy_modulkalem ADD
    kdvtutar DECIMAL(18,4) NULL, kdvoran DECIMAL(18,4) NULL,
    tutar DECIMAL(18,4) NULL, subeid NVARCHAR(64) NULL,
    kayitgiristarih DATETIME NULL, rezervasyondanaktarildi INT NULL;
GO

PRINT 'loadtransfer tablolari hazir';
GO
